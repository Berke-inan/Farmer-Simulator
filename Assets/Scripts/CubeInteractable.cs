using Unity.Netcode;
using UnityEngine;

public class CubeInteractable : NetworkBehaviour, IInteractable
{
    [Header("Ayarlar")]
    public GameObject spherePrefab;

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            // EĞER OYUNCUNUN ELİNDE ZATEN HERHANGİ BİR EŞYA VARSA İŞLEM YAPMA
            if (inventory.aktifAlet != ToolType.Yok)
            {
                Debug.Log("Eliniz zaten dolu, yeni tohum alınamaz!");
                return;
            }

            SpawnSphereServerRpc(interactor.OwnerClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnSphereServerRpc(ulong clientId)
    {
        if (spherePrefab == null) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
                // Hata payına karşı oyuncunun elinde asılı kalmış bir obje varsa önce onu sil 
                if (inventory.eldekiObje != null && inventory.eldekiObje.IsSpawned)
                {
                    inventory.eldekiObje.Despawn();
                }

                // Tohumu sandığın hemen üstünde spawnla
                GameObject spawnedSphere = Instantiate(spherePrefab, transform.position + Vector3.up, Quaternion.identity);
                spawnedSphere.SetActive(true);

                if (spawnedSphere.TryGetComponent(out NetworkObject sphereObj))
                {
                    sphereObj.SpawnWithOwnership(clientId);

                    // Yeni sisteme göre objeyi doğrudan ele ver ve takibi başlat
                    if (spawnedSphere.TryGetComponent(out PickupableTool tool))
                    {
                        tool.isEquipped.Value = true;

                        // DİKKAT: tohumID ve tohumMiktari kısımları silindi. 
                        // Tohumun kendi özellikleri artık kendi üzerindeki TohumEylemi scriptinde yaşıyor.
                        inventory.AletKusanServerRpc(sphereObj, tool.aletTipi);
                    }
                }
            }
        }
    }
}