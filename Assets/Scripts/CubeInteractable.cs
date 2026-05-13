using Unity.Netcode;
using UnityEngine;

public class CubeInteractable : NetworkBehaviour, IInteractable
{
    [Header("Eşya Ayarları")]
    public int verilecekItemID = 101; // string yerine int yapıldı
    public int miktar = 1;

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            // Sunucuya "Bana bu itemı ver" komutunu gönder
            GiveItemServerRpc(interactor.NetworkObjectId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void GiveItemServerRpc(ulong oyuncuID)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuID, out NetworkObject oyuncuNetObj))
        {
            PlayerInventory inventory = oyuncuNetObj.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                // PlayerInventory içindeki AddLocalItemClientRpc metodunu tetikleyen bir yapı
                // Burada PlayerInventory'e yeni bir metod eklemek en temizidir
                inventory.GiveSpecificItemServerRpc(verilecekItemID, miktar);
            }
        }
    }
}