using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{
    public static DeliveryManager Instance;

    [Header("Alan Ayarları")]
    public Collider deliveryZoneCollider;

    [Header("Market Verileri")]
    public MarketItem[] allAvailableItems;

    [Header("Debug/Testing")]
    public bool isTesting = false;

    // Sunucuda tutulan anlık stoklar ve zamanlayıcılar
    private Dictionary<int, int> serverStocks = new Dictionary<int, int>();
    private Dictionary<int, float> restockTimers = new Dictionary<int, float>();

    private List<GameObject> pendingDeliveries = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Stokları ve zamanlayıcıları başlangıç değerleriyle doldur
            foreach (var item in allAvailableItems)
            {
                int initialStock = item.isMachine ? 1 : item.maxStock;
                serverStocks[item.itemID] = initialStock;
                restockTimers[item.itemID] = 0f;
            }
        }

        // Oyuna yeni giren client mevcut stok durumunu sunucudan istesin diye 
        // ya da ilk açılışta güncel stokları çekmek için sunucu başlangıçta herkese gönderir
        if (IsServer)
        {
            NotifyAllClientsAboutStocks();
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        // Her eşya için zamanlayıcıyı çalıştır
        foreach (var item in allAvailableItems)
        {
            int maxAllowed = item.isMachine ? 1 : item.maxStock;

            // Eğer stok maksimumda değilse zamanı ilerlet
            if (serverStocks[item.itemID] < maxAllowed)
            {
                restockTimers[item.itemID] += Time.deltaTime;

                if (restockTimers[item.itemID] >= item.restockInterval)
                {
                    serverStocks[item.itemID]++;
                    restockTimers[item.itemID] = 0f; // Zamanlayıcıyı sıfırla

                    // Stok değiştiği için tüm istemcilere bildir
                    SyncStockRpc(item.itemID, serverStocks[item.itemID]);
                }
            }
            else
            {
                restockTimers[item.itemID] = 0f; // Stok tam ise zamanlayıcıyı sıfırla
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PurchaseCartRpc(int[] cartItemIDs, ulong clientId)
    {
        int totalCost = 0;
        List<GameObject> itemsToBuy = new List<GameObject>();

        // Geçici olarak bu işlemde hangi üründen kaç tane ekleneceğini simüle edelim
        Dictionary<int, int> requestedQuantities = new Dictionary<int, int>();
        foreach (int id in cartItemIDs)
        {
            if (!requestedQuantities.ContainsKey(id)) requestedQuantities[id] = 0;
            requestedQuantities[id]++;
        }

        // Stok ve Fiyat Doğrulama
        foreach (var kvp in requestedQuantities)
        {
            int id = kvp.Key;
            int qty = kvp.Value;
            MarketItem item = GetItemByID(id);

            if (item == null || serverStocks[id] < qty)
            {
                // Stok yetersizse işlemi iptal et ve client'a haber ver
                SendPurchaseResultRpc(false, "Stok yetersiz veya ürün bulunamadı!", RpcTarget.Single(clientId, RpcTargetUse.Temp));
                return;
            }

            totalCost += item.price * qty;
            for (int i = 0; i < qty; i++)
            {
                itemsToBuy.Add(item.prefabToSpawn);
            }
        }

        // Para Doğrulama
        if (EconomyManager.Instance.currentMoney >= totalCost)
        {
            // Parayı sadece sunucu düşüyor (Güvenli yöntem)
            EconomyManager.Instance.currentMoney -= totalCost;

            // Stokları kalıcı olarak düş ve herkese senkronize et
            foreach (var kvp in requestedQuantities)
            {
                serverStocks[kvp.Key] -= kvp.Value;
                SyncStockRpc(kvp.Key, serverStocks[kvp.Key]);
            }

            pendingDeliveries.AddRange(itemsToBuy);
            if (isTesting) DeliverPendingItems();

            SendPurchaseResultRpc(true, "Ödeme başarılı! Siparişiniz hazırlanıyor.", RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
        else
        {
            SendPurchaseResultRpc(false, "Yetersiz bakiye!", RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SyncStockRpc(int itemID, int newStock)
    {
        if (MarketUIController.IsMarketOpen || true)
        {
            // Arayüze güncel stoğu gönderir
            MarketUIController.Instance.UpdateItemStock(itemID, newStock);
        }
    }

    // [SendTo.SpecifiedInParams] kullanımı için RpcParams parametresi eklendi
    [Rpc(SendTo.SpecifiedInParams)]
    private void SendPurchaseResultRpc(bool success, string message, RpcParams rpcParams = default)
    {
        MarketUIController.Instance.OnPurchaseResponse(success, message);
    }

    private void NotifyAllClientsAboutStocks()
    {
        foreach (var kvp in serverStocks)
        {
            SyncStockRpc(kvp.Key, kvp.Value);
        }
    }

    public void DeliverPendingItems()
    {
        if (!IsServer || pendingDeliveries.Count == 0) return;
        foreach (GameObject prefab in pendingDeliveries)
        {
            Vector3 center = deliveryZoneCollider.bounds.center;
            Vector3 extents = deliveryZoneCollider.bounds.extents;
            Vector3 randomPos = new Vector3(
                Random.Range(center.x - extents.x, center.x + extents.x),
                center.y,
                Random.Range(center.z - extents.z, center.z + extents.z)
            );
            GameObject spawnedItem = Instantiate(prefab, randomPos, Quaternion.identity);
            spawnedItem.GetComponent<NetworkObject>().Spawn();
        }
        pendingDeliveries.Clear();
    }

    public List<SellableItem> GetItemsInZone()
    {
        List<SellableItem> items = new List<SellableItem>();
        Collider[] hitColliders = Physics.OverlapBox(deliveryZoneCollider.bounds.center, deliveryZoneCollider.bounds.extents, deliveryZoneCollider.transform.rotation);
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<SellableItem>(out SellableItem s)) items.Add(s);
        }
        return items;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SellSelectedItemsRpc(ulong[] networkObjectIds)
    {
        int totalEarned = 0;
        foreach (ulong id in networkObjectIds)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj))
            {
                if (obj.TryGetComponent(out SellableItem s))
                {
                    totalEarned += s.GetPrice();
                    obj.Despawn();
                }
            }
        }
        if (totalEarned > 0) EconomyManager.Instance.AddMoney(totalEarned);
    }

    private MarketItem GetItemByID(int id)
    {
        foreach (var item in allAvailableItems)
            if (item.itemID == id) return item;
        return null;
    }
}