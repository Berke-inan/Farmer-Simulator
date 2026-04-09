using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class TractorController : NetworkBehaviour, IInteractable
{
    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform visualFL;
    public Transform visualFR;
    public Transform visualBL;
    public Transform visualBR;

    [Header("Fiziksel Tekerlekler (Wheel Colliders)")]
    public WheelCollider wcFL;
    public WheelCollider wcFR;
    public WheelCollider wcBL;
    public WheelCollider wcBR;

    [Header("Fizik Ayarları")]
    public Transform centerOfMass;

    [Header("Motor Ayarları")]
    public float motorTorque = 1500f;
    public float maxSteerAngle = 30f;
    public float brakeForce = 3000f;
    public float maxSpeedKmh = 70f;

    [Header("Traktöre Binme Ayarları")]
    public Transform driverSeat;
    public Camera tpsCamera;

    private InputSystem_Actions inputActions;
    private float gasBrakeInput;
    private float steeringInput;
    private bool isBraking;
    private Rigidbody rb;

    private NetworkObject currentDriver;
    public bool IsOccupied => currentDriver != null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        if (tpsCamera != null) tpsCamera.gameObject.SetActive(false);

        rb = GetComponent<Rigidbody>();

        if (centerOfMass != null)
        {
            GetComponent<Rigidbody>().centerOfMass = centerOfMass.localPosition;
        }
    }

    public void Interact(NetworkObject interactor)
    {
        if (!IsOccupied)
        {
            MountTractorServerRpc(interactor.NetworkObjectId);
        }
        else if (currentDriver == interactor)
        {
            DismountTractorServerRpc();
        }
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
                if (tpsCamera != null) tpsCamera.gameObject.SetActive(true);
            }
        }
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
            currentDriver.transform.position = transform.position + transform.right * -2f;

            if (currentDriver.IsOwner)
            {
                inputActions.Player.Disable();
                if (tpsCamera != null) tpsCamera.gameObject.SetActive(false);
            }

            TogglePlayerComponents(currentDriver, true);
            currentDriver = null;
        }
    }

    private void TogglePlayerComponents(NetworkObject player, bool state)
    {
        if (player.TryGetComponent(out PlayerMovement movement)) movement.enabled = state;
        if (player.TryGetComponent(out CharacterController characterController)) characterController.enabled = state;

        Camera playerCamera = player.GetComponentInChildren<Camera>();
        if (playerCamera != null && player.IsOwner)
        {
            playerCamera.gameObject.SetActive(state);
        }
    }

    private void Update()
    {
        if (wcFL != null && visualFL != null) UpdateSingleWheel(wcFL, visualFL);
        if (wcFR != null && visualFR != null) UpdateSingleWheel(wcFR, visualFR);
        if (wcBL != null && visualBL != null) UpdateSingleWheel(wcBL, visualBL);
        if (wcBR != null && visualBR != null) UpdateSingleWheel(wcBR, visualBR);
    }

    private void FixedUpdate()
    {
        if (!IsOccupied || !IsOwner) return;

        gasBrakeInput = inputActions.Player.GasBrake.ReadValue<float>();
        steeringInput = inputActions.Player.Steering.ReadValue<float>();

        isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        float currentTorque = gasBrakeInput * motorTorque;

        if (isBraking)
        {
            wcFL.brakeTorque = brakeForce;
            wcFR.brakeTorque = brakeForce;
            wcBL.brakeTorque = brakeForce;
            wcBR.brakeTorque = brakeForce;

            wcFL.motorTorque = 0f;
            wcFR.motorTorque = 0f;
            wcBL.motorTorque = 0f;
            wcBR.motorTorque = 0f;
        }
        else
        {
            wcFL.brakeTorque = 0f;
            wcFR.brakeTorque = 0f;
            wcBL.brakeTorque = 0f;
            wcBR.brakeTorque = 0f;

            // DÖNÜŞLERDE GÜÇ KAYBINI ÖNLEME
            float turnCompensation = 1f + (Mathf.Abs(steeringInput) * 2.0f);
            float compensatedTorque = currentTorque * turnCompensation;

            // ARACIN ŞU ANKİ HIZINI KM/S CİNSİNDEN HESAPLAMA
            // Unity 6'da linearVelocity kullanıyoruz, büyüklüğünü (magnitude) 3.6 ile çarparak km/s buluyoruz.
            float currentSpeedKmh = rb.linearVelocity.magnitude * 3.6f;

            // HIZ SINIRI KONTROLÜ
            if (currentSpeedKmh < maxSpeedKmh)
            {
                // Sınırın altındaysak gücü ön tekerleklere ver
                wcFL.motorTorque = compensatedTorque;
                wcFR.motorTorque = compensatedTorque;
            }
            else
            {
                // Hız sınırına ulaşıldıysa motor gücünü kes (araç kendi momentumuyla süzülür)
                wcFL.motorTorque = 0f;
                wcFR.motorTorque = 0f;
            }

            // ARKA TEKERLEK KİLİTLENMESİNİ (DRAG) ÖNLEME
            float antiDragTorque = (Mathf.Abs(gasBrakeInput) > 0.1f) ? 0.001f : 0f;
            wcBL.motorTorque = antiDragTorque;
            wcBR.motorTorque = antiDragTorque;
        }

        float currentSteerAngle = steeringInput * maxSteerAngle;

        // ACKERMANN GEOMETRİSİ
        // Tekerleğin çok fazla yan dönmesini engellemek için çarpan 1.3f'ten 1.15f'e düşürüldü.
        if (steeringInput > 0.1f)
        {
            wcFL.steerAngle = currentSteerAngle;
            wcFR.steerAngle = currentSteerAngle * 1.15f;
        }
        else if (steeringInput < -0.1f)
        {
            wcFL.steerAngle = currentSteerAngle * 1.15f;
            wcFR.steerAngle = currentSteerAngle;
        }
        else
        {
            wcFL.steerAngle = currentSteerAngle;
            wcFR.steerAngle = currentSteerAngle;
        }
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform visualWheel)
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visualWheel.position = pos;
        visualWheel.rotation = rot;
    }
}