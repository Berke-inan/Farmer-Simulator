using Unity.Netcode;
using UnityEngine;

public class SphereFollower : NetworkBehaviour
{
    [Header("Takip Ayarları")]
    public Vector3 offset = new Vector3(0.8f, 0f, 1f); // Kameraya göre ofset
    public float followSpeed = 10f; // Kamera hızlı döneceği için takip hızı biraz artırıldı

    private Transform targetCamera;

    [Rpc(SendTo.Everyone)]
    public void SetTargetClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
            {
                // Oyuncunun üzerindeki PlayerInteractor scriptine ulaşıp kamerayı çekiyoruz
                PlayerInteractor interactor = client.PlayerObject.GetComponent<PlayerInteractor>();

                if (interactor != null && interactor.playerCamera != null)
                {
                    targetCamera = interactor.playerCamera; // Artık hedefimiz gövde değil, kamera!
                }
                else
                {
                    // Eğer bulamazsa yedeğe (gövdeye) geç
                    targetCamera = client.PlayerObject.transform;
                }
            }
        }
    }

    void Update()
    {
        // Hedef kamera belirlenmişse yumuşak bir şekilde takip et
        if (targetCamera != null)
        {
            // Artık kameranın dönüşüne (yukarı/aşağı bakış dahil) göre ofset hesaplanıyor
            Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(offset);

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        }
    }
}