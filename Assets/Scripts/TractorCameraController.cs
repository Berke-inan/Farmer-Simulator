using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class TractorCameraController : NetworkBehaviour
{
    [Header("Kamera Referansı")]
    public CinemachineCamera tractorCam;

    [Header("Dinamik Takip Noktası")]
    [Tooltip("Traktörün içindeki boş CameraTarget objesini buraya sürükleyin")]
    public Transform cameraTarget;

    [Header("Öncelik Ayarları")]
    public int activePriority = 20;

    // YENİ: Kamerayı döndüren bileşenin referansı
    private CinemachineInputAxisController _axisController;

    // Awake yerine Netcode'un güvenli başlama metodunu kullanıyoruz!
    public override void OnNetworkSpawn()
    {
        if (tractorCam != null)
        {
            // Kamerayı prefabdan çıkarıp ana sahneye (root) alıyoruz
            // Artık ağ bağlantısı kurulduktan sonra yaptığımız için Netcode kamerayı silmeyecek.
            tractorCam.transform.SetParent(null);

            // DİKKAT: Obje her zaman AÇIK kalacak. Kapatma/Açma yok!
            tractorCam.gameObject.SetActive(true);

            // Başlangıçta önceliğini 0 yaparak sırasını beklemesini sağlıyoruz
            tractorCam.Priority = 0;

            // YENİ: Dönüş bileşenini bul ve hafızaya al
            _axisController = tractorCam.GetComponent<CinemachineInputAxisController>();

            // Cinemachine'e takip hedeflerini veriyoruz
            if (cameraTarget != null)
            {
                tractorCam.Follow = cameraTarget;
                tractorCam.LookAt = cameraTarget;
            }
        }
    }

    private void Update()
    {
        // KESİN ÇÖZÜM: Menü açıksa kamera dönüşünü tamamen kilitle!
        if (_axisController != null)
        {
            _axisController.enabled = !FarmerSimulator.UI.MainMenuController.IsMenuOpen;
        }
    }

    public void SetCameraActive(bool isActive)
    {
        if (tractorCam != null)
        {
            // Sadece öncelik (Priority) yükselip alçalıyor
            tractorCam.Priority = isActive ? activePriority : 0;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (tractorCam != null)
        {
            Destroy(tractorCam.gameObject);
        }
    }
}