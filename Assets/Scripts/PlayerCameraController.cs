using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerCameraController : NetworkBehaviour
{
    public float mouseSensitivity = 15f;
    public Transform cameraRoot;

    [Header("Oyuncu Sanal Kamerası")]
    public CinemachineCamera playerCinemachineCam;

    [Header("Binek Durumu")]
    public bool isRiding = false;

    private InputSystem_Actions inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float yRotation = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();

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

        if (FarmerSimulator.UI.MainMenuController.IsMenuOpen || FarmerSimulator.UI.ChatController.IsChatOpen) return;

        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 75f);

        if (isRiding)
        {
            // --- KESİN ÇÖZÜM BURASI ---
            // Sadece farenin x eksenindeki hareketini yRotation'a ekliyoruz.
            yRotation += mouseX;

            // "localRotation" YERİNE "rotation" KULLANIYORUZ!
            // Bu sayede kamera atın hareketlerinden tamamen bağımsızlaşır. 
            // Sen nereye bakarsan crosshair oraya kilitlenir, at altından o yöne doğru kendi döner.
            cameraRoot.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
        else
        {
            // --- YAYA MODU ---
            transform.Rotate(Vector3.up * mouseX);

            // Attan inip binerken kameranın aniden sıçramasını/yön değiştirmesini engellemek için,
            // kameranın dünya üzerindeki Y açısını sürekli hafızaya (yRotation) kaydediyoruz.
            yRotation = cameraRoot.eulerAngles.y;

            // Yürürken bedene bağlı kalması için tekrar localRotation kullanıyoruz.
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}