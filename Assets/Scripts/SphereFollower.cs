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
        // Obje henüz ağda tam olarak spawn olmadıysa işlem yapılmaz
        if (!IsSpawned) return;

        // Hedef kamera henüz bulunamadıysa arama işlemi yapılır
        if (targetCamera == null)
        {
            // Ağ üzerinden objenin sahibini (Owner) bul
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);

            if (playerObj != null)
            {
                // Sahibin üzerindeki PlayerInteractor scriptinden kameraya ulaş
                if (playerObj.TryGetComponent(out PlayerInteractor interactor) && interactor.playerCamera != null)
                {
                    targetCamera = interactor.playerCamera;
                }
                else
                {
                    // Kamera yoksa gövde hedef alınır
                    targetCamera = playerObj.transform;
                }
            }

            // Arama bitti, takibe bir sonraki karede başlanır
            return;
        }

        // Hedef kamera bulunduğunda yumuşak takip (Lerp) işlemi
        Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
    }
}