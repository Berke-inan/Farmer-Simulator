using Unity.Netcode;
using UnityEngine;

public class CubeInteractable : NetworkBehaviour, IInteractable
{
    [Header("Eşya Ayarları")]
    [Tooltip("ItemDatabase içindeki ItemID ile birebir aynı olmalı!")]
    public string verilecekItemID = "CornSeed";
    public int miktar = 1;

    public void Interact(NetworkObject interactor)
    {
        // Oyuncunun üzerindeki yeni managerları bul
        if (interactor.TryGetComponent(out InventoryManager inventory) &&
            interactor.TryGetComponent(out NetworkedHotbar hotbar))
        {
            // O anki aktif slotun indeksini al (Eline direkt gelsin diye lazım)
            int activeSlot = hotbar.ActiveSlotIndex.Value;

            // Sunucuya "Bana bu itemı ver" komutunu gönder
            GiveItemServerRpc(interactor.NetworkObjectId, activeSlot);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void GiveItemServerRpc(ulong oyuncuID, int activeSlot)
    {
        // Ağ üzerindeki oyuncu objesini bul
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuID, out NetworkObject oyuncuNetObj))
        {
            if (oyuncuNetObj.TryGetComponent(out InventoryManager inventory))
            {
                // 1. Veritabanından mısır tohumu verisini çek
                ItemData cornData = ItemDatabase.Instance.GetItemByID(verilecekItemID);

                if (cornData == null)
                {
                    Debug.LogError($"<color=red>[HATA]</color> {verilecekItemID} ID'li eşya veritabanında bulunamadı!");
                    return;
                }

                // 2. Envanterde yer var mı kontrol et
                if (inventory.HasSpaceFor(cornData, miktar))
                {
                    // 3. Envantere ekle (Senin istediğin gibi: Eğer elindeki slot boşsa direkt eline gelecek)
                    inventory.AddItemServer(cornData, miktar, activeSlot);

                    Debug.Log($"<color=green>[Cube]</color> {oyuncuNetObj.name} oyuncusuna {cornData.ItemName} verildi.");
                }
                else
                {
                    Debug.LogWarning("<color=yellow>[Cube]</color> Oyuncunun envanteri dolu!");
                }
            }
        }
    }
}