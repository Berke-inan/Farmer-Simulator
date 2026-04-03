using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class TractorController : MonoBehaviour
{
    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform visualFL; // Ön Sol (Front Left)
    public Transform visualFR; // Ön Sað (Front Right)
    public Transform visualBL; // Arka Sol (Back Left)
    public Transform visualBR; // Arka Sað (Back Right)

    [Header("Fiziksel Tekerlekler (Wheel Colliders)")]
    public WheelCollider wcFL;
    public WheelCollider wcFR;
    public WheelCollider wcBL;
    public WheelCollider wcBR;

    [Header("Motor Ayarlarý")]
    public float motorTorque = 1500f;  // Traktörün motor gücü
    public float maxSteerAngle = 30f;  // Direksiyonun maksimum dönme açýsý
    public float brakeForce = 3000f;   // Fren yapma gücü

    [Header("Traktöre Binme Ayarlarý")]
    public Transform driverSeat;       // Sürücü oturma pozisyonu
    public float interactionDistance = 3f; // Traktöre yaklaþma mesafesi
    public string interactionKey = "f"; // Traktöre binmek için tuþ

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool isBraking;
    private NetworkObject currentDriver;
    private bool isOccupied;
    private Vector3 driverOriginalPosition;
    private Quaternion driverOriginalRotation;

    public bool IsOccupied => isOccupied;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        isOccupied = false;
    }

    private void OnEnable() { inputActions.Player.Enable(); }
    private void OnDisable() { inputActions.Player.Disable(); }

    private void FixedUpdate()
    {
        // Eðer traktör dolu ise kontrolü uygulamaya baþla
        if (!isOccupied) return;

        // 1. Girdileri Al (WASD tuþlarýný okur)
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        // Boþluk (Space) tuþuna basýlýp basýlmadýðýný kontrol et
        isBraking = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        // 2. Motor Gücünü Hesapla (Sadece W ve S tuþlarýndan gelen Y ekseni verisi)
        float currentTorque = moveInput.y * motorTorque;

        // 3. Gücü ve Freni Arka Tekerleklere Uygula (Arkadan Ýtiþli Sistem)
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

        // 4. Direksiyon Açýsýný Ön Tekerleklere Uygula (A ve D tuþlarýndan gelen X ekseni verisi)
        float currentSteerAngle = moveInput.x * maxSteerAngle;
        wcFL.steerAngle = currentSteerAngle;
        wcFR.steerAngle = currentSteerAngle;

        // 5. Görsel Tekerlekleri, Görünmez Fiziksel Tekerleklerle Eþitle
        UpdateSingleWheel(wcFL, visualFL);
        UpdateSingleWheel(wcFR, visualFR);
        UpdateSingleWheel(wcBL, visualBL);
        UpdateSingleWheel(wcBR, visualBR);

        // 6. Sürücünün konumunu güncelle
        if (isOccupied && currentDriver != null && driverSeat != null)
        {
            currentDriver.transform.position = driverSeat.position;
            currentDriver.transform.rotation = driverSeat.rotation;
        }
    }

    public void MountTractor(NetworkObject player)
    {
        if (isOccupied) return;

        isOccupied = true;
        currentDriver = player;

        // Oyuncunun orijinal konumunu kaydet
        driverOriginalPosition = player.transform.position;
        driverOriginalRotation = player.transform.rotation;

        // Oyuncuyu traktörün sürücü konumuna taþý
        if (driverSeat != null)
        {
            player.transform.position = driverSeat.position;
            player.transform.rotation = driverSeat.rotation;
        }

        // Oyuncu komponentlerini devre dýþý býrak
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        PlayerCameraController cameraController = player.GetComponent<PlayerCameraController>();
        CharacterController characterController = player.GetComponent<CharacterController>();

        if (playerMovement != null) playerMovement.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        if (characterController != null) characterController.enabled = false;
    }

    public void DismountTractor()
    {
        if (currentDriver == null || !isOccupied) return;

        isOccupied = false;

        // Oyuncuyu traktörün önüne býrak
        currentDriver.transform.position = transform.position + transform.forward * 2f;
        currentDriver.transform.rotation = transform.rotation;

        // Oyuncu komponentlerini aktif et
        PlayerMovement playerMovement = currentDriver.GetComponent<PlayerMovement>();
        PlayerCameraController cameraController = currentDriver.GetComponent<PlayerCameraController>();
        CharacterController characterController = currentDriver.GetComponent<CharacterController>();

        if (playerMovement != null) playerMovement.enabled = true;
        if (cameraController != null) cameraController.enabled = true;
        if (characterController != null) characterController.enabled = true;

        currentDriver = null;
    }

    // Fizik motorundaki tekerleðin pozisyonunu ve dönme açýsýný alýp, 3D modele aktaran fonksiyon
    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform visualWheel)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);

        visualWheel.position = pos;
        visualWheel.rotation = rot;
    }
}