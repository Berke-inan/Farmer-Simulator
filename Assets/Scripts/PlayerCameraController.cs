using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerCameraController : NetworkBehaviour
{
    public float mouseSensitivity = 15f;

    public Transform cameraRoot;

    [Header("Oyuncu Sanal Kamerası")]
    public CinemachineCamera playerCinemachineCam; // Gerçek Camera değil, Cinemachine kamerası

    private InputSystem_Actions inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();

            // BİZ DOĞDUK: Kendi sanal kameramızı açıp önceliğini 10 yapıyoruz.
            // Bu sayede Priority'si 5 olan Lobby kamerasını anında ezip görüntüyü devralıyoruz!
            if (playerCinemachineCam != null)
            {
                playerCinemachineCam.gameObject.SetActive(true);
                playerCinemachineCam.Priority = 10;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // BAŞKA OYUNCU DOĞDU: Bizim ekranımızda onun kamerasının yeri yok, tamamen kapatıyoruz.
            if (playerCinemachineCam != null)
            {
                playerCinemachineCam.Priority = 0;
                playerCinemachineCam.gameObject.SetActive(false);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    void LateUpdate()
    {
        if (!IsOwner) return;

        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 75f);
        cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}