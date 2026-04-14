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

    [Header("Traktöre Binme Ayarları")]
    public Transform driverSeat;
    public TractorCameraController cameraController;

    [Header("Ağ Tekerlek Senkronizasyonu")]
    public NetworkVariable<float> netSteerAngle = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> netWheelRPM = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // --- YENİ EKLENEN: Diğer scriptlerin okuyabileceği sürüş bilgileri ---
    public float CurrentGasInput { get; private set; }
    public bool IsDrivenByMe => currentDriver != null && IsOwner;
    // ----------------------------------------------------------------------

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
            currentDriver.transform.position = transform.position + transform.right * -4f;
            if (currentDriver.IsOwner)
            {
                inputActions.Player.Interact.started -= OnInteractPressed;
                inputActions.Player.Disable();
                if (cameraController != null) cameraController.SetCameraActive(false);
            }
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
            return;
        }

        CurrentGasInput = inputActions.Player.GasBrake.ReadValue<float>();
        steeringInput = inputActions.Player.Steering.ReadValue<float>();
        isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        float currentTorque = CurrentGasInput * motorTorque;

        if (isBraking)
        {
            wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = brakeForce;
            wcFL.motorTorque = wcFR.motorTorque = wcBL.motorTorque = wcBR.motorTorque = 0f;
        }
        else
        {
            wcFL.brakeTorque = wcFR.brakeTorque = wcBL.brakeTorque = wcBR.brakeTorque = 0f;

            float turnCompensation = 1f + (Mathf.Abs(steeringInput) * 0.2f);
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

        float currentSteerAngle = steeringInput * maxSteerAngle;
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