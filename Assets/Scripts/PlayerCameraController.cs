using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerCameraController : NetworkBehaviour
{
    public float mouseSensitivity = 15f;

    public Transform cameraRoot;
    public Camera playerCamera;
    public GameObject cinemachineCamera;

    private InputSystem_Actions inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();

            if (playerCamera != null) playerCamera.gameObject.SetActive(true);
            if (cinemachineCamera != null) cinemachineCamera.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
            if (cinemachineCamera != null) cinemachineCamera.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Karakterin vücudunu sağa/sola döndür
        transform.Rotate(Vector3.up * mouseX);

        // Kameranın kök objesini (Kafayı) yukarı/aşağı döndür
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}