using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{
    public static DeliveryManager Instance;

    [Header("Alan Ayarları")]
    [Tooltip("Eşyaların taranacağı ve teslim edileceği isTrigger Collider")]
    public Collider deliveryZoneCollider;

    [Header("Market Verileri")]
    public MarketItem[] allAvailableItems;

    [Header("Debug/Testing")]
    public bool isTesting = false;

    private List<GameObject> pendingDeliveries = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PurchaseCartRpc(int[] cartItemIDs, ulong clientId)
    {
        int totalCost = 0;
        List<GameObject> itemsToBuy = new List<GameObject>();

        foreach (int id in cartItemIDs)
        {
            MarketItem item = GetItemByID(id);
            if (item != null)
            {
                totalCost += item.price;
                itemsToBuy.Add(item.prefabToSpawn);
            }
        }

        if (EconomyManager.Instance.currentMoney >= totalCost)
        {
            EconomyManager.Instance.currentMoney -= totalCost;
            pendingDeliveries.AddRange(itemsToBuy);

            if (isTesting) DeliverPendingItems();
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
        Collider[] hitColliders = Physics.OverlapBox(
            deliveryZoneCollider.bounds.center,
            deliveryZoneCollider.bounds.extents,
            deliveryZoneCollider.transform.rotation
        );

        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent<SellableItem>(out SellableItem s))
                items.Add(s);
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