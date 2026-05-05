using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{
    public static DeliveryManager Instance;

    [Header("Teslimat ve Satış Alanı")]
    [Tooltip("Adım 1'de oluşturduğun isTrigger olan DeliveryZone objesinin Collider'ını buraya sürükle")]
    public Collider deliveryZoneCollider;

    [Header("Market Ayarları")]
    public MarketItem[] allAvailableItems; // İleride MarketItem dosyalarımızı buraya atacağız

    // Sunucuda bekleyen siparişler
    private List<GameObject> pendingDeliveries = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ================= 1. SATIN ALMA SİSTEMİ =================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PurchaseCartRpc(int[] cartItemIDs, ulong clientId)
    {
        int totalCost = 0;
        List<GameObject> itemsToBuy = new List<GameObject>();

        // Sepetteki ID'lerden eşyaları bul ve toplam fiyatı hesapla
        foreach (int id in cartItemIDs)
        {
            MarketItem item = GetItemByID(id);
            if (item != null)
            {
                totalCost += item.price;
                itemsToBuy.Add(item.prefabToSpawn);
            }
        }

        // Para kontrolü
        if (EconomyManager.Instance.currentMoney >= totalCost)
        {
            EconomyManager.Instance.currentMoney -= totalCost; // Parayı kes
            pendingDeliveries.AddRange(itemsToBuy); // Kargo bekleme listesine ekle
            Debug.Log($"Sipariş onaylandı. Toplam: {totalCost}. Sabah 6'da gelecek.");
        }
        else
        {
            Debug.Log("Yetersiz bakiye!");
        }
    }

    // Sabah 6 olduğunda çalışacak kargo getirme metodu
    public void DeliverPendingItems()
    {
        if (!IsServer || pendingDeliveries.Count == 0) return;

        foreach (GameObject prefab in pendingDeliveries)
        {
            // Adım 1'de yaptığın Collider'ın içinde rastgele bir pozisyon bulur
            Vector3 center = deliveryZoneCollider.bounds.center;
            Vector3 extents = deliveryZoneCollider.bounds.extents;

            Vector3 randomPos = new Vector3(
                Random.Range(center.x - extents.x, center.x + extents.x),
                center.y,
                Random.Range(center.z - extents.z, center.z + extents.z)
            );

            // Obgeyi doğur ve ağda spawn et
            GameObject spawnedItem = Instantiate(prefab, randomPos, Quaternion.identity);
            spawnedItem.GetComponent<NetworkObject>().Spawn();
        }

        pendingDeliveries.Clear();
        Debug.Log("Kargolar teslim edildi!");
    }

    private MarketItem GetItemByID(int id)
    {
        foreach (var item in allAvailableItems)
        {
            if (item.itemID == id) return item;
        }
        return null;
    }

    // ================= 2. SATIŞ SİSTEMİ =================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SellItemsInZoneRpc()
    {
        // Teslimat alanının (Collider'ın) içindeki TÜM objeleri sanal bir kutu ile tarar
        Collider[] hitColliders = Physics.OverlapBox(
            deliveryZoneCollider.bounds.center,
            deliveryZoneCollider.bounds.extents,
            deliveryZoneCollider.transform.rotation
        );

        int totalEarned = 0;

        foreach (Collider hit in hitColliders)
        {
            // Alanın içindeki objede "SellableItem" scripti var mı?
            if (hit.TryGetComponent<SellableItem>(out SellableItem sellable))
            {
                totalEarned += sellable.price;

                // Varsa ağ üzerinden güvenli bir şekilde sil
                NetworkObject netObj = sellable.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
            }
        }

        // Eğer satılan bir şeyler varsa parayı topluca ekle
        if (totalEarned > 0)
        {
            EconomyManager.Instance.AddMoney(totalEarned);
            Debug.Log($"Alandaki eşyalar satıldı. Toplam Kazanılan: {totalEarned}");
        }
        else
        {
            Debug.Log("Satış alanında satılabilecek hiçbir eşya bulunamadı.");
        }
    }
}