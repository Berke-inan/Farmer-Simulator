using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Oyuncu Sanal Kamerası")]
    public float mouseSensitivity = 15f;
    public Transform cameraRoot;

    [Header("Oyuncu Sanal Kamerası")]
    public CinemachineCamera playerCinemachineCam;

    [Header("Binek Durumu")]
    public bool isRiding = false;
    private bool lastRidingState = false; // Değişimi takip etmek için

    private Renderer[] allRenderers; // Performans için parçaları hafızada tutuyoruz

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

            // Tüm render parçalarını bir kereye mahsus hafızaya alıyoruz (Performans dostu)
            allRenderers = GetComponentsInChildren<Renderer>();

            // Başlangıçta yaya olduğumuz için vücudu gizle (Sadece gölge)
            UpdateBodyVisibility(false);

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

        // --- BİNME/İNME DURUMUNU KONTROL ET ---
        // isRiding durumu dışarıdan (traktör veya at scriptinden) değiştirildiğinde anında tetiklenir
        if (isRiding != lastRidingState)
        {
            lastRidingState = isRiding;
            UpdateBodyVisibility(isRiding); // Binildiyse görünür yap, indiyse gizle
        }

        if (FarmerSimulator.UI.MainMenuController.IsMenuOpen || FarmerSimulator.UI.ChatController.IsChatOpen) return;

        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 75f);

        if (isRiding)
        {
            yRotation += mouseX;
            cameraRoot.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
            yRotation = cameraRoot.eulerAngles.y;
            cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    // Görünürlüğü değiştiren yardımcı fonksiyon
    private void UpdateBodyVisibility(bool showFullBody)
    {
        if (allRenderers == null) return;

        foreach (Renderer rend in allRenderers)
        {
            if (rend != null)
            {
                // showFullBody true ise normal çiz (On), false ise sadece gölge yap (ShadowsOnly)
                rend.shadowCastingMode = showFullBody ?
                    UnityEngine.Rendering.ShadowCastingMode.On :
                    UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    public void SetRidingState(bool state)
    {
        isRiding = state;
        lastRidingState = state;
        UpdateBodyVisibility(state);
    }
}