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

    [Header("Traktöre Binme Ayarları")]
    public Transform driverSeat;
    public Camera tpsCamera;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool isBraking;

    private NetworkObject currentDriver;
    public bool IsOccupied => currentDriver != null;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        if (tpsCamera != null) tpsCamera.gameObject.SetActive(false);

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

    // YENİ SİSTEM: Herkesin sunucuya istek atabilmesi için InvokePermission.Everyone kullanılıyor
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

    // YENİ SİSTEM: Sunucudan tüm istemcilere (Client) bilgi göndermek için SendTo.Everyone kullanılıyor
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

    private void FixedUpdate()
    {
        // 1. Görsel tekerlek güncellemesi HER ZAMAN, herkes için çalışmalı (return'den önceye aldık)
        UpdateSingleWheel(wcFL, visualFL);
        UpdateSingleWheel(wcFR, visualFR);
        UpdateSingleWheel(wcBL, visualBL);
        UpdateSingleWheel(wcBR, visualBR);

        // Eğer traktör boşsa veya bu bilgisayarın traktör üzerinde yetkisi yoksa, sürüş hesaplamalarını yapma
        if (!IsOccupied || !IsOwner) return;

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        float currentTorque = moveInput.y * motorTorque;

        if (isBraking)
        {
            wcBL.brakeTorque = brakeForce;
            wcBR.brakeTorque = brakeForce;
            wcBL.motorTorque = 0f;
            wcBR.motorTorque = 0f;
        }
        else
        {
            wcBL.brakeTorque = 0f;
            wcBR.brakeTorque = 0f;
            wcBL.motorTorque = currentTorque;
            wcBR.motorTorque = currentTorque;
        }

        float currentSteerAngle = moveInput.x * maxSteerAngle;
        wcFL.steerAngle = currentSteerAngle;
        wcFR.steerAngle = currentSteerAngle;
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform visualWheel)
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        visualWheel.position = pos;
        visualWheel.rotation = rot;
    }
}