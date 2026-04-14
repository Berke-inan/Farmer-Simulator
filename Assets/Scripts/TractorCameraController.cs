using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class TractorCameraController : NetworkBehaviour
{
    [Header("Kamera Referansı")]
    public CinemachineCamera tractorCam;

    [Header("Öncelik Ayarları")]
    public int activePriority = 20;

    private void Awake()
    {
        if (tractorCam != null)
        {
            // Kamerayı prefabdan çıkarıp ana sahneye (root) alıyoruz
            tractorCam.transform.SetParent(null);

            // DİKKAT: Obje her zaman AÇIK kalacak. Kapatma/Açma yok!
            tractorCam.gameObject.SetActive(true);

            // Başlangıçta önceliğini 0 yaparak sırasını beklemesini sağlıyoruz
            tractorCam.Priority = 0;
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