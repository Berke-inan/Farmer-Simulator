using Unity.Netcode;
using UnityEngine;

public class EquippedItemFollower : NetworkBehaviour
{
    [Header("Takip Ayarları")]
    // Çapa ve su kabı kameranın biraz daha altında ve sağında dursun diye offset değiştirilebilir
    public Vector3 offset = new Vector3(0.5f, -0.4f, 1f);
    public float followSpeed = 10f;

    private Transform targetCamera;

    void Update()
    {
        if (!IsSpawned) return;

        if (targetCamera == null)
        {
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);

            if (playerObj != null)
            {
                if (playerObj.TryGetComponent(out PlayerInteractor interactor) && interactor.playerCamera != null)
                {
                    targetCamera = interactor.playerCamera;
                }
                else
                {
                    targetCamera = playerObj.transform;
                }
            }
            return;
        }

        // Pozisyonu takip et
        Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Rotasyonu takip et (Eşya hep kameranın baktığı yöne baksın)
        transform.rotation = Quaternion.Lerp(transform.rotation, targetCamera.rotation, Time.deltaTime * followSpeed);
    }
}