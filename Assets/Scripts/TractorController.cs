using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class TractorController : NetworkBehaviour, IInteractable
{
    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform visualFL, visualFR, visualBL, visualBR;

    [Header("Fiziksel Tekerlekler (Wheel Colliders)")]
    public WheelCollider wcFL, wcFR, wcBL, wcBR;

    [Header("Fizik Ayarları")]
    public Transform centerOfMass;

    [Header("Motor Ayarları")]
    public float motorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float maxSpeedKmh = 70f;

    // --- YENİ EKLENEN: Direksiyon Dönüş Hızı ---
    [Tooltip("Direksiyonun ne kadar hızlı döneceği (Düşük sayı = Daha yavaş ve ağır direksiyon)")]
    public float steerSpeed = 1.5f;
    private float smoothedSteeringInput = 0f; // Mevcut yumuşatılmış girdi
    // ------------------------------------------

    [Header("Traktöre Binme Ayarları")]
    public Transform driverSeat;
    public TractorCameraController cameraController;

    [Header("Ağ Tekerlek Senkronizasyonu")]
    public NetworkVariable<float> netSteerAngle = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> netWheelRPM = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float CurrentGasInput { get; private set; }
    public bool IsDrivenByMe => currentDriver != null && IsOwner;

    private float clientSpinAngle = 0f;
    private InputSystem_Actions inputActions;
    private float steeringInput;
    private bool isBraking;
    private Rigidbody rb;
    private NetworkObject currentDriver;

    public bool IsOccupied => currentDriver != null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null) rb.centerOfMass = centerOfMass.localPosition;
    }

    public void Interact(NetworkObject interactor)
    {
        if (!IsOccupied) MountTractorServerRpc(interactor.NetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MountTractorServerRpc(ulong playerId)
    {
        if (IsOccupied) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            GetComponent<NetworkObject>().ChangeOwnership(playerObj.OwnerClientId);
            playerObj.TrySetParent(transform);
            MountTractorClientRpc(playerId);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void MountTractorClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            currentDriver = playerObj;
            playerObj.transform.position = driverSeat.position;
            playerObj.transform.rotation = driverSeat.rotation;
            TogglePlayerComponents(playerObj, false);

            if (playerObj.IsOwner)
            {
                inputActions.Player.Enable();
                inputActions.Player.Interact.started += OnInteractPressed;
                if (cameraController != null) cameraController.SetCameraActive(true);
            }
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (IsOccupied && IsOwner) DismountTractorServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DismountTractorServerRpc()
    {
        if (currentDriver != null)
        {
            GetComponent<NetworkObject>().RemoveOwnership();
            currentDriver.TryRemoveParent();
            DismountTractorClientRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void DismountTractorClientRpc()
    {
        if (currentDriver != null)
        {
            // 1. Traktörün yönüne göre yatayda (Zemin hizasında) Sol tarafı bul
            Vector3 safeLeft = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.left;

            // 2. Hedef X ve Z koordinatını traktörün 3.5 metre solu olarak belirle
            Vector3 targetXZ = transform.position + (safeLeft * 3.5f);

            // 3. KESİN ÇÖZÜM: Lazer (Raycast) ile yerin gerçek yüksekliğini bul
            // Hedef noktanın 10 metre yukarısından (Havadan) aşağıya doğru bir lazer atıyoruz
            Vector3 rayStart = new Vector3(targetXZ.x, transform.position.y + 10f, targetXZ.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f))
            {
                // ZEMİN BULUNDU: Karakteri bulduğu zeminin (hit.point) tam 1.5 metre yukarısına bırakır.
                // Not: CharacterController ayağı yere 1 mm bile girse yerin altına düşer! 
                // O yüzden 1.5m havadan bırakıyoruz, yerçekimi onu 0.1 saniyede yumuşakça çimlere oturtur.
                currentDriver.transform.position = hit.point + (Vector3.up * 1.5f);
            }
            else
            {
                // Eğer lazer hiçbir şeye çarpmazsa (Örn: Uçurum kenarıysa) acil durum konumu
                currentDriver.transform.position = targetXZ + (Vector3.up * 2f);
            }

            // Oyuncuyu dimdik (Yamulmamış) şekilde ayağa kaldır
            currentDriver.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            if (currentDriver.IsOwner)
            {
                inputActions.Player.Interact.started -= OnInteractPressed;
                inputActions.Player.Disable();
                if (cameraController != null) cameraController.SetCameraActive(false);
            }

            // Konumu sabitledikten sonra fizik kapsülünü geri aç
            TogglePlayerComponents(currentDriver, true);
            currentDriver = null;
        }
    }

    private void TogglePlayerComponents(NetworkObject player, bool state)
    {
        if (player.TryGetComponent(out PlayerMovement movement)) movement.enabled = state;
        if (player.TryGetComponent(out CharacterController characterController)) characterController.enabled = state;
        if (player.TryGetComponent(out PlayerInteractor interactor)) interactor.enabled = state;

        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator != null) animator.SetBool("isDriving", !state);

        if (player.IsOwner)
        {
            if (player.TryGetComponent(out PlayerCameraController camController)) camController.enabled = state;
            Unity.Cinemachine.CinemachineCamera playerCam = player.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);
            if (playerCam != null) playerCam.Priority = state ? 10 : 0;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (wcFL != null && visualFL != null) UpdateSingleWheel(wcFL, visualFL);
            if (wcFR != null && visualFR != null) UpdateSingleWheel(wcFR, visualFR);
            if (wcBL != null && visualBL != null) UpdateSingleWheel(wcBL, visualBL);
            if (wcBR != null && visualBR != null) UpdateSingleWheel(wcBR, visualBR);
        }
        else
        {
            AnimateWheelsForClient();
        }
    }

    private void AnimateWheelsForClient()
    {
        float degreesPerSecond = netWheelRPM.Value * 360f / 60f;
        clientSpinAngle += degreesPerSecond * Time.deltaTime;

        if (wcFL != null) wcFL.steerAngle = netSteerAngle.Value;
        if (wcFR != null) wcFR.steerAngle = netSteerAngle.Value;

        if (wcFL != null && visualFL != null) UpdateClientWheel(wcFL, visualFL, clientSpinAngle);
        if (wcFR != null && visualFR != null) UpdateClientWheel(wcFR, visualFR, clientSpinAngle);
        if (wcBL != null && visualBL != null) UpdateClientWheel(wcBL, visualBL, clientSpinAngle);
        if (wcBR != null && visualBR != null) UpdateClientWheel(wcBR, visualBR, clientSpinAngle);
    }

    private void UpdateClientWheel(WheelCollider wc, Transform visual, float spinAngle)
    {
        wc.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visual.position = pos;
        visual.rotation = rot * Quaternion.Euler(spinAngle, 0f, 0f);
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (wcFL != null)
        {
            netWheelRPM.Value = wcFL.rpm;
            netSteerAngle.Value = wcFL.steerAngle;
        }

        if (!IsOccupied)
        {
            CurrentGasInput = 0f;
            smoothedSteeringInput = 0f; // Kimse yoksa direksiyonu merkeze çek

            // ==========================================
            // İNİNCE TRAKTÖRÜ ÇİVİLE
            // Traktörden inildiği an tüm tekerlere tam fren basılır.
            // ==========================================
            if (wcFL != null)
            {
                wcFL.motorTorque = wcFR.motorTorque = wcBL.motorTorque = wcBR.motorTorque = 0f;
                wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = brakeForce;
            }
            return;
        }

        CurrentGasInput = inputActions.Player.GasBrake.ReadValue<float>();
        steeringInput = inputActions.Player.Steering.ReadValue<float>();
        isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        smoothedSteeringInput = Mathf.MoveTowards(smoothedSteeringInput, steeringInput, Time.fixedDeltaTime * steerSpeed);

        // ==========================================
        // YENİ EKLENEN: AKTİF YÖN FRENİ SİSTEMİ
        // ==========================================
        float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        bool isDirectionBraking = false;

        // EĞER ileri gidiyorsa (hız > 1) VE oyuncu geriye (S) basıyorsa
        // VEYA geri gidiyorsa (hız < -1) VE oyuncu ileriye (W) basıyorsa
        if ((localForwardSpeed > 1f && CurrentGasInput < -0.1f) || (localForwardSpeed < -1f && CurrentGasInput > 0.1f))
        {
            isDirectionBraking = true;
        }

        float currentTorque = CurrentGasInput * motorTorque;

        // El freni veya yön freni tetiklendiyse durdur
        if (isBraking || isDirectionBraking)
        {
            // W/S ile yapılan fren normal el freninden daha güçlü tutsun (x1.5)
            float activeBrakeForce = isDirectionBraking ? brakeForce * 1.5f : brakeForce;

            wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = activeBrakeForce;
            wcFL.motorTorque = wcFR.motorTorque = wcBL.motorTorque = wcBR.motorTorque = 0f;
        }
        else
        {
            wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = 0f;

            float turnCompensation = 1f + (Mathf.Abs(smoothedSteeringInput) * 0.2f);
            float compensatedTorque = currentTorque * turnCompensation;

            if (rb.linearVelocity.magnitude * 3.6f < maxSpeedKmh)
            {
                wcFL.motorTorque = wcFR.motorTorque = compensatedTorque;
            }
            else
            {
                wcFL.motorTorque = wcFR.motorTorque = 0f;
            }

            float antiDragTorque = (Mathf.Abs(CurrentGasInput) > 0.1f) ? 0.001f : 0f;
            wcBL.motorTorque = wcBR.motorTorque = antiDragTorque;
        }

        float currentSteerAngle = smoothedSteeringInput * maxSteerAngle;
        if (smoothedSteeringInput > 0.1f)
        {
            wcFL.steerAngle = currentSteerAngle;
            wcFR.steerAngle = currentSteerAngle * 1.15f;
        }
        else if (smoothedSteeringInput < -0.1f)
        {
            wcFL.steerAngle = currentSteerAngle * 1.15f;
            wcFR.steerAngle = currentSteerAngle;
        }
        else
        {
            wcFL.steerAngle = wcFR.steerAngle = currentSteerAngle;
        }
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform visualWheel)
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visualWheel.position = pos;
        visualWheel.rotation = rot;
    }
}