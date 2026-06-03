using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using System.Collections.Generic;

public class TractorController : NetworkBehaviour, IInteractable
{
    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform visualFL, visualFR, visualBL, visualBR;

    [Header("Fiziksel Tekerlekler (Wheel Colliders)")]
    public WheelCollider wcFL, wcFR, wcBL, wcBR;

    [Header("Fizik Ayarları")]
    public Transform centerOfMass;

    public float CurrentSpeedKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f; // Hızı dışarıya açıyoruz
    private TractorDashboardUI dashboardUI;

    [Header("Motor Ayarları")]
    public float motorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float maxSpeedKmh = 70f;

    [Tooltip("Direksiyonun ne kadar hızlı döneceği (Düşük sayı = Daha yavaş ve ağır direksiyon)")]
    public float steerSpeed = 1.5f;
    private float smoothedSteeringInput = 0f;

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

    private TractorFuelSystem fuelSystem;

    public bool IsOccupied => currentDriver != null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        rb = GetComponent<Rigidbody>();
        if (centerOfMass != null) rb.centerOfMass = centerOfMass.localPosition;

        fuelSystem = GetComponent<TractorFuelSystem>();
        dashboardUI = GetComponent<TractorDashboardUI>();
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
            TogglePlayerComponents(playerObj, false); // Kapatma emrini ver

            // Sadece süren yerel oyuncu için (Input ve Kamera)
            if (playerObj.IsOwner)
            {
                if (dashboardUI != null) dashboardUI.ToggleDashboard(true);
                // Sürüş tuşlarını aktif et
                inputActions.Player.Enable();
                inputActions.Player.Interact.started += OnInteractPressed;

                // Traktör kamerasını aç (Öncelik 20)
                if (cameraController != null) cameraController.SetCameraActive(true);

                // UI İpuçlarını zorla HUD'a bas
                if (FarmerSimulator.UI.HUDManager.Instance != null)
                {
                    List<ActionPrompt> drivingPrompts = new List<ActionPrompt>()
                    {
                        new ActionPrompt("T", "MOTOR"),
                        new ActionPrompt("F", "ALET TAK/ÇIKAR"),
                        new ActionPrompt("V", "ALETİ ÇALIŞTIR"),
                        new ActionPrompt("Space", "EL FRENİ"),
                        new ActionPrompt("E", "İN")
                    };
                    FarmerSimulator.UI.HUDManager.Instance.UpdateActionPrompts(drivingPrompts);
                }
            }
            if (FarmerSimulator.UI.HUDManager.Instance != null)
                FarmerSimulator.UI.HUDManager.Instance.SetPlayerHUDVisible(false);

            if (dashboardUI != null) dashboardUI.ToggleDashboard(true);
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {

        if (FarmerSimulator.UI.MainMenuController.IsMenuOpen) return;

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
            // Sadece süren yerel oyuncu için (Input ve Kamera)
            if (currentDriver.IsOwner)
            {
                if (dashboardUI != null) dashboardUI.ToggleDashboard(false);
                inputActions.Player.Interact.started -= OnInteractPressed;
                inputActions.Player.Disable();
                if (cameraController != null) cameraController.SetCameraActive(false);

                // HUD'ı temizle
                if (FarmerSimulator.UI.HUDManager.Instance != null)
                {
                    FarmerSimulator.UI.HUDManager.Instance.UpdateActionPrompts(new List<ActionPrompt>());
                }

                if (FarmerSimulator.UI.HUDManager.Instance != null)
                    FarmerSimulator.UI.HUDManager.Instance.SetPlayerHUDVisible(true);

                // Traktör Dashboard'unu kapat
                if (dashboardUI != null) dashboardUI.ToggleDashboard(false);
            }

            // Oyuncuyu güvenli bir yere çıkar
            Vector3 safeLeft = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.left;
            Vector3 targetXZ = transform.position + (safeLeft * 3.5f);
            Vector3 rayStart = new Vector3(targetXZ.x, transform.position.y + 10f, targetXZ.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f))
            {
                currentDriver.transform.position = hit.point + (Vector3.up * 1.5f);
            }
            else
            {
                currentDriver.transform.position = targetXZ + (Vector3.up * 2f);
            }

            currentDriver.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            TogglePlayerComponents(currentDriver, true); // Bileşenleri geri aç
            currentDriver = null;
        }
    }

    public List<ActionPrompt> GetPrompts()
    {
        string eylemMetni = IsOccupied ? "DOLU" : "BİN";
        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", eylemMetni)
        };
    }

    // ========================================================
    // KESİN ÇÖZÜM: HATASIZ BİLEŞEN AÇMA/KAPAMA SİSTEMİ
    // ========================================================
    private void TogglePlayerComponents(NetworkObject playerObj, bool state)
    {
        // 1. GENEL BİLEŞENLER (Fizik ve Animasyon - Herkes için)

        // CharacterController (Unity'nin kendi yeşil kapsülü)
        CharacterController cc = playerObj.GetComponentInChildren<CharacterController>();
        if (cc != null) cc.enabled = state;

        // PlayerMovement (Senin yürüme kodun)
        PlayerMovement movement = playerObj.GetComponentInChildren<PlayerMovement>();
        if (movement != null) movement.enabled = state;

        // Animator (Child objelerde aranıyor)
        Animator animator = playerObj.GetComponentInChildren<Animator>();
        if (animator != null) animator.SetBool("isDriving", !state);


        // 2. SADECE YEREL OYUNCU BİLEŞENLERİ (isOwner kontrolü)
        if (playerObj.IsOwner)
        {
            // PlayerInteractor (Kameradaki Raycast kodu - KESİN KAPANMALI!)
            PlayerInteractor interactor = playerObj.GetComponentInChildren<PlayerInteractor>();
            if (interactor != null) interactor.enabled = state;

            // PlayerCameraController (Karakterin kendi kamera kontrol scripti)
            PlayerCameraController camController = playerObj.GetComponentInChildren<PlayerCameraController>();
            if (camController != null) camController.enabled = state;

            // Envanter ve Eldeki Eşya Görünürlüğü (ZORT DİYEN ÇİZGİLERİN ÇÖZÜMÜ)
            PlayerInventory inventory = playerObj.GetComponentInChildren<PlayerInventory>();
            if (inventory != null)
            {
                // state = false ise biniyoruz demektir -> Eşyayı sakla
                inventory.SetHolstered(!state);

                // KESİN ÇÖZÜM: Sadece kılıflama yetmez, objeyi tamamen KAPA!
                if (inventory.eldekiObje != null)
                {
                    inventory.eldekiObje.SetActive(state); // state false ise obje kapanır
                }
            }

            // Karakterin kendi Cinemachine Kamerasının önceliğini düşür (2 bindiğimizde traktör kamerasına geçebilsin)
            Unity.Cinemachine.CinemachineCamera playerCam = playerObj.GetComponentInChildren<Unity.Cinemachine.CinemachineCamera>(true);
            if (playerCam != null) playerCam.Priority = state ? 10 : 0;
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            // Motor Çalıştırma (T Tuşu)
            if (IsOccupied &&
                 currentDriver != null &&
                 currentDriver.IsOwner &&
                 !FarmerSimulator.UI.MainMenuController.IsMenuOpen && // YENİ KİLİT
                 Keyboard.current != null &&
                 Keyboard.current.tKey.wasPressedThisFrame)
            {
                if (fuelSystem != null)
                    fuelSystem.ToggleEngineServerRpc();
            }

            // Fiziksel tekerleklerin yerini görsel tekerleklere aktar
            if (wcFL != null && visualFL != null) UpdateSingleWheel(wcFL, visualFL);
            if (wcFR != null && visualFR != null) UpdateSingleWheel(wcFR, visualFR);
            if (wcBL != null && visualBL != null) UpdateSingleWheel(wcBL, visualBL);
            if (wcBR != null && visualBR != null) UpdateSingleWheel(wcBR, visualBR);
        }
        else
        {
            // Diğer oyuncularda tekerlekleri döndür (Client-Side Prediction)
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
            smoothedSteeringInput = 0f;

            if (wcFL != null)
            {
                wcFL.motorTorque = wcFR.motorTorque = wcBL.motorTorque = wcBR.motorTorque = 0f;
                wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = brakeForce;
            }
            return;
        }

        // Motor Kapalıysa WASD Engelleme
        if (fuelSystem != null && !fuelSystem.isEngineRunning.Value)
        {
            CurrentGasInput = 0f;
            smoothedSteeringInput = 0f;

            if (wcFL != null)
            {
                wcFL.motorTorque = wcFR.motorTorque = wcBL.motorTorque = wcBR.motorTorque = 0f;
                wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = brakeForce;
            }
            return;
        }

        // ==========================================
        // YENİ KİLİT: MENÜ AÇIKKEN KONTROLLERİ DEVRE DIŞI BIRAK
        // (Traktör momentumuyla akmaya devam eder)
        // ==========================================
        if (FarmerSimulator.UI.MainMenuController.IsMenuOpen)
        {
            CurrentGasInput = 0f; // Ayağını gazdan çek
            steeringInput = 0f;   // Direksiyonu bırak
            isBraking = false;    // Frene basma (Araç kendi kendine süzülür)
        }
        else
        {
            // Normal Sürüş Mantığı
            CurrentGasInput = inputActions.Player.GasBrake.ReadValue<float>();
            if (fuelSystem != null && !fuelSystem.HasFuel) CurrentGasInput = 0f;

            steeringInput = inputActions.Player.Steering.ReadValue<float>();
            isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        }
        // ==========================================

        smoothedSteeringInput = Mathf.MoveTowards(smoothedSteeringInput, steeringInput, Time.fixedDeltaTime * steerSpeed);

        float localForwardSpeed = transform.InverseTransformDirection(rb.linearVelocity).z;
        bool isDirectionBraking = false;

        if ((localForwardSpeed > 1f && CurrentGasInput < -0.1f) || (localForwardSpeed < -1f && CurrentGasInput > 0.1f))
        {
            isDirectionBraking = true;
        }

        float currentTorque = CurrentGasInput * motorTorque;

        if (isBraking || isDirectionBraking)
        {
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