using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class Romork : NetworkBehaviour, IInteractable
{
    [Header("Dizilim Ayarları")]
    public Transform kargoNoktasi;

    [Header("Grid Kapasitesi")]
    public int sutunSayisi = 2;
    public int satirSayisi = 3;
    public int maksimumKat = 2;

    [Header("Mesafe Ayarları")]
    public float aralikX = 2.2f;
    public float aralikZ = 0.6f;
    public float yiginYuksekligi = 0.6f;

    // Römork içindeki fiziksel objeleri tutan yığın
    private Stack<NetworkObject> icindekiEsyalar = new Stack<NetworkObject>();

    // ==========================================
    // DİNAMİK HUD TUŞ İPUÇLARI (IInteractable)
    // ==========================================
    public List<ActionPrompt> GetPrompts()
    {
        List<ActionPrompt> prompts = new List<ActionPrompt>();
        int maksimumKapasite = sutunSayisi * satirSayisi * maksimumKat;

        // Yerel oyuncunun eline bakıyoruz (Sadece o anki oyuncunun ekranını etkiler)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
                int aktifSlotIdx = inventory.activeHotbarIndex.Value;
                InventorySlot aktifSlot = inventory.slots[aktifSlotIdx];

                if (!aktifSlot.IsEmpty)
                {
                    // Oyuncunun ELİ DOLU (Yükleme Senaryosu)
                    if (icindekiEsyalar.Count >= maksimumKapasite)
                        prompts.Add(new ActionPrompt("E", "RÖMORK DOLU"));
                    else
                        prompts.Add(new ActionPrompt("E", "YÜKLE"));
                }
                else
                {
                    // Oyuncunun ELİ BOŞ (Alma Senaryosu)
                    if (icindekiEsyalar.Count > 0)
                        prompts.Add(new ActionPrompt("E", "AL"));
                    else
                        prompts.Add(new ActionPrompt("E", "RÖMORK BOŞ"));
                }

                return prompts;
            }
        }

        // Yedek durum
        prompts.Add(new ActionPrompt("E", "ETKİLEŞİM"));
        return prompts;
    }

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            int aktifSlotIdx = inventory.activeHotbarIndex.Value;
            InventorySlot aktifSlot = inventory.slots[aktifSlotIdx];

            // 1. DURUM: Elimiz doluysa römorka koymaya çalış
            if (!aktifSlot.IsEmpty)
            {
                int maksimumKapasite = sutunSayisi * satirSayisi * maksimumKat;
                if (icindekiEsyalar.Count >= maksimumKapasite)
                {
                    Debug.Log("Römork tamamen dolu!");
                    return;
                }

                // Elimizdeki eşyanın ID'sini sunucuya yolluyoruz
                RomorkaKoyServerRpc(interactor.NetworkObjectId, aktifSlot.itemData.itemID, aktifSlotIdx);
            }
            // 2. DURUM: Elimiz boşsa römorktan en üstteki eşyayı al
            else
            {
                if (icindekiEsyalar.Count > 0)
                {
                    RomorktanAlServerRpc(interactor.NetworkObjectId);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RomorkaKoyServerRpc(ulong playerNetId, int itemID, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject playerObj))
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            ItemData data = Resources.Load<ItemData>("Items/" + itemID);

            if (data != null && data.groundPrefab != null)
            {
                // Fiziksel objeyi römork için spawn et
                GameObject kargo = Instantiate(data.groundPrefab);
                NetworkObject kargoNetObj = kargo.GetComponent<NetworkObject>();
                kargoNetObj.Spawn();
                kargoNetObj.TrySetParent(transform);

                // Fizik ve Collider'ı kapat (Römorkta sabit dursun)
                if (kargo.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
                if (kargo.TryGetComponent(out Collider col)) col.enabled = false;

                // Dizilim hesaplama
                int sira = icindekiEsyalar.Count;
                int katKapasitesi = sutunSayisi * satirSayisi;
                int katIndex = sira / katKapasitesi;
                int katIciSira = sira % katKapasitesi;
                int xIndex = katIciSira % sutunSayisi;
                int zIndex = katIciSira / sutunSayisi;

                Vector3 yerelOffset = new Vector3(xIndex * aralikX, katIndex * yiginYuksekligi, -(zIndex * aralikZ));
                kargo.transform.localPosition = kargoNoktasi.localPosition + yerelOffset;
                kargo.transform.localRotation = kargoNoktasi.localRotation;

                icindekiEsyalar.Push(kargoNetObj);

                // Envanterden 1 tane düşür
                inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RomorktanAlServerRpc(ulong playerNetId)
    {
        if (icindekiEsyalar.Count == 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject playerObj))
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            NetworkObject alinanKargo = icindekiEsyalar.Pop();

            if (alinanKargo != null && alinanKargo.TryGetComponent(out InteractableItem item))
            {
                // Envantere ekle
                inventory.GiveSpecificItemServerRpc(item.itemID, 1);

                // Sahadaki fiziksel objeyi yok et
                alinanKargo.Despawn();
            }
        }
    }
}