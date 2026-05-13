using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Envanter Ayarları")]
    public int maxSlots = 10;
    public InventorySlot[] slots;

    [Header("El Görselleri")]
    public Transform handTransform;
    public GameObject eldekiObje;

    public NetworkVariable<int> activeHotbarIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public event Action<int, InventorySlot> OnSlotChanged;

    private void Awake()
    {
        if (slots == null || slots.Length != maxSlots)
        {
            slots = new InventorySlot[maxSlots];
            for (int i = 0; i < maxSlots; i++) slots[i] = new InventorySlot();
        }
    }

    public override void OnNetworkSpawn()
    {
        activeHotbarIndex.OnValueChanged += HandleHotbarChanged;
        UpdateHeldItemVisuals(activeHotbarIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        activeHotbarIndex.OnValueChanged -= HandleHotbarChanged;
    }

    private void HandleHotbarChanged(int previousIndex, int newIndex)
    {
        UpdateHeldItemVisuals(newIndex);
    }

    // --- YENİ: Zıplatma İsteği ---
    [Rpc(SendTo.Server)]
    public void RequestBounceServerRpc(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out Rigidbody rb))
            {
                // Eşyayı havaya zıplatma fiziği
                rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestPickupServerRpc(ulong networkObjectId, RpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj)) return;

        if (netObj.TryGetComponent(out InteractableItem groundItem))
        {
            int idToSend = groundItem.itemID;
            int amountToSend = groundItem.amount;
            netObj.Despawn();

            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
            };

            AddLocalItemClientRpc(idToSend, amountToSend, clientRpcParams);
        }
    }

    [ClientRpc]
    private void AddLocalItemClientRpc(int itemID, int amount, ClientRpcParams clientRpcParams = default)
    {
        if (ItemRegistry.Instance == null || ItemRegistry.Instance.itemDatabase == null) return;
        ItemData fetchedData = ItemRegistry.Instance.itemDatabase.GetItemByID(itemID);
        if (fetchedData == null) return;

        // --- DEĞİŞİKLİK: Sadece aktif slota bakıyoruz ---
        int i = activeHotbarIndex.Value;

        // 1. Durum: Slot aynı eşya ile dolu mu ve stacklenebilir mi?
        if (!slots[i].IsEmpty && slots[i].itemData == fetchedData && slots[i].amount < fetchedData.maxStack)
        {
            int space = fetchedData.maxStack - slots[i].amount;
            int addAmount = Mathf.Min(space, amount);
            slots[i].amount += addAmount;
            OnSlotChanged?.Invoke(i, slots[i]);
            UpdateHeldItemVisuals(i);
        }
        // 2. Durum: Slot boş mu?
        else if (slots[i].IsEmpty)
        {
            slots[i].AddItem(fetchedData, amount);
            OnSlotChanged?.Invoke(i, slots[i]);
            UpdateHeldItemVisuals(i);
        }
    }

    public void ChangeHotbarSlot(int index)
    {
        if (!IsOwner) return;
        if (index >= 0 && index < maxSlots)
        {
            activeHotbarIndex.Value = index;
        }
    }

    private void UpdateHeldItemVisuals(int slotIndex)
    {
        if (eldekiObje != null) Destroy(eldekiObje);
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        InventorySlot currentSlot = slots[slotIndex];
        if (currentSlot.IsEmpty || currentSlot.itemData == null || currentSlot.itemData.heldModelPrefab == null) return;

        eldekiObje = Instantiate(currentSlot.itemData.heldModelPrefab, handTransform);
        eldekiObje.transform.localPosition = currentSlot.itemData.holdPositionOffset;
        eldekiObje.transform.localEulerAngles = currentSlot.itemData.holdRotationOffset;
    }

    // --- YENİ: Slot Dolu mu Kontrolü (Local) ---
    public bool CanPickupToActiveSlot(int itemID)
    {
        InventorySlot activeSlot = slots[activeHotbarIndex.Value];
        if (activeSlot.IsEmpty) return true;

        if (activeSlot.itemData.itemID == itemID && activeSlot.amount < activeSlot.itemData.maxStack)
            return true;

        return false;
    }

    public void EldekiniYereAt()
    {
        if (!IsOwner) return;
        InventorySlot activeSlot = slots[activeHotbarIndex.Value];
        if (activeSlot.IsEmpty) return;

        int idToDrop = activeSlot.itemData.itemID;
        activeSlot.amount--;

        if (activeSlot.amount <= 0)
        {
            activeSlot.ClearSlot();
            UpdateHeldItemVisuals(activeHotbarIndex.Value);
        }

        OnSlotChanged?.Invoke(activeHotbarIndex.Value, activeSlot);
        Transform camTransform = GetComponent<PlayerInteractor>().playerCamera;
        DropItemServerRpc(idToDrop, handTransform.position, camTransform.forward);
    }

    [Rpc(SendTo.Server)]
    private void DropItemServerRpc(int itemID, Vector3 dropPosition, Vector3 forwardDirection, RpcParams rpcParams = default)
    {
        if (ItemRegistry.Instance == null || ItemRegistry.Instance.itemDatabase == null) return;
        ItemData data = ItemRegistry.Instance.itemDatabase.GetItemByID(itemID);
        if (data == null || data.groundPrefab == null) return;

        Vector3 spawnPos = dropPosition + forwardDirection * 1.5f;
        GameObject droppedObj = Instantiate(data.groundPrefab, spawnPos, Quaternion.identity);
        NetworkObject netObj = droppedObj.GetComponent<NetworkObject>();
        netObj.Spawn();

        if (droppedObj.TryGetComponent(out Rigidbody rb))
        {
            rb.AddForce(forwardDirection * 2f + Vector3.up * 2f, ForceMode.Impulse);
        }
    }
    [Header("Sepet Verileri")]
    public NetworkVariable<int> sepetDoluluk = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Rpc(SendTo.Server)]
    public void ToplamaIstegiServerRpc(ulong agacID)
    {
        if (sepetDoluluk.Value >= 2) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(agacID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TreeController agac))
            {
                if (agac.MeyveHasatEt()) sepetDoluluk.Value++;
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void YakitAktarServerRpc(ulong traktorID, float miktar, ulong istasyonID)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject tNet) &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(istasyonID, out NetworkObject iNet))
        {
            var traktor = tNet.GetComponent<TractorFuelSystem>();
            var istasyon = iNet.GetComponent<YakitIstasyonu>();

            float gercektenCekilen = istasyon.YakitCek(miktar);
            if (gercektenCekilen > 0) traktor.AddFuelServerRpc(gercektenCekilen);
        }
    }

    [Rpc(SendTo.Server)]
    public void EldeTuketimYapServerRpc()
    {
        int index = activeHotbarIndex.Value;
        if (slots[index].IsEmpty) return;

        slots[index].amount--;
        if (slots[index].amount <= 0) slots[index].ClearSlot();

        TuketimSonucuClientRpc(index, slots[index].amount);
    }

    [Rpc(SendTo.Everyone)]
    private void TuketimSonucuClientRpc(int index, int newAmount)
    {
        if (newAmount <= 0)
        {
            slots[index].ClearSlot();
            UpdateHeldItemVisuals(index);
        }
        else
        {
            slots[index].amount = newAmount;
        }
        OnSlotChanged?.Invoke(index, slots[index]);
    }

    [Header("Alet Verileri")]
    public NetworkVariable<float> bidonMevcutYakit = new NetworkVariable<float>(25f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Rpc(SendTo.Server)]
    public void BidondanTraktoreServerRpc(ulong traktorID, float miktar)
    {
        if (bidonMevcutYakit.Value < miktar) miktar = bidonMevcutYakit.Value;
        if (miktar <= 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TractorFuelSystem traktor))
            {
                float bosYer = traktor.maxFuel - traktor.currentFuel.Value;
                float eklenecek = Mathf.Min(miktar, bosYer);

                if (eklenecek > 0)
                {
                    traktor.AddFuelServerRpc(eklenecek);
                    bidonMevcutYakit.Value -= eklenecek;
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void DecreaseItemAmountServerRpc(int slotIndex, int count)
    {
        if (slots[slotIndex].IsEmpty) return;

        slots[slotIndex].amount -= count;
        if (slots[slotIndex].amount <= 0) slots[slotIndex].ClearSlot();

        DecreaseItemClientRpc(slotIndex, slots[slotIndex].amount);
    }

    [Rpc(SendTo.Everyone)]
    private void DecreaseItemClientRpc(int slotIndex, int newAmount)
    {
        if (newAmount <= 0)
        {
            slots[slotIndex].ClearSlot();
            UpdateHeldItemVisuals(slotIndex);
        }
        else
        {
            slots[slotIndex].amount = newAmount;
        }
        OnSlotChanged?.Invoke(slotIndex, slots[slotIndex]);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GiveSpecificItemServerRpc(int itemID, int amount, RpcParams rpcParams = default)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { rpcParams.Receive.SenderClientId } }
        };

        AddLocalItemClientRpc(itemID, amount, clientRpcParams);
    }

    [Rpc(SendTo.Server)]
    public void DikmeIstegiServerRpc(Vector3 nokta, int slotIndex)
    {
        if (slots[slotIndex].IsEmpty || slots[slotIndex].itemData == null) return;

        ItemData data = slots[slotIndex].itemData;

        if (data.groundPrefab != null)
        {
            GameObject yeniFide = Instantiate(data.groundPrefab, nokta + (Vector3.up * 0.05f), Quaternion.identity);
            NetworkObject netObj = yeniFide.GetComponent<NetworkObject>();
            netObj.Spawn();

            DecreaseItemAmountServerRpc(slotIndex, 1);
        }
    }
}