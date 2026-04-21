using Unity.Netcode;
using UnityEngine;

public class SphereFollower : NetworkBehaviour
{
    [Header("Takip Ayarları")]
    public Vector3 offset = new Vector3(0.8f, 0f, 1f);
    public float followSpeed = 10f;

    private Transform targetCamera;

    void Update()
    {
        // Obje henüz ağda tam olarak spawn olmadıysa işlem yapma
        if (!IsSpawned) return;

        // Hedef kamera henüz bulunamadıysa bulmayı dene
        if (targetCamera == null)
        {
            // RPC ile gelen veri yerine, objenin doğrudan sahibini (OwnerClientId) kullan
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out NetworkClient client))
            {
                if (client.PlayerObject != null)
                {
                    PlayerInteractor interactor = client.PlayerObject.GetComponent<PlayerInteractor>();

                    if (interactor != null && interactor.playerCamera != null)
                    {
                        targetCamera = interactor.playerCamera;
                    }
                    else
                    {
                        // Kamera bulunamazsa yedek olarak gövdeyi hedef al
                        targetCamera = client.PlayerObject.transform;
                    }
                }
            }
            return; // Arama işlemi tamamlandığında bir sonraki frame (kare) takip başlayacak
        }

        // Hedef kamera bulunduysa yumuşak bir şekilde takip et
        Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
    }
}