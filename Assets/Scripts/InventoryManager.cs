using System;
using Unity.Netcode;
using UnityEngine;

public class InventoryManager : NetworkBehaviour
{
    [SerializeField] private int inventorySize = 10;
    public InventorySlot[] Slots;
    public event Action<int, InventorySlot> OnSlotUpdated;

    private void Awake()
    {
        Slots = new InventorySlot[inventorySize];
        // Slotları başlatmayı unutmayalım
        for (int i = 0; i < Slots.Length; i++) Slots[i] = new InventorySlot(null, 0);
    }

    // --- EKSİK OLAN HASSPACEFOR METODU ---
    public bool HasSpaceFor(ItemData item, int amount)
    {
        int remainingAmount = amount;
        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].IsEmpty) return true;
            if (Slots[i].Item == item && Slots[i].Amount < item.MaxStack)
            {
                int spaceInSlot = item.MaxStack - Slots[i].Amount;
                remainingAmount -= spaceInSlot;
                if (remainingAmount <= 0) return true;
            }
        }
        return false;
    }

    public void AddItemServer(ItemData item, int amount, int activeSlotIndex)
    {
        if (!IsServer) return;

        Debug.Log($"<color=cyan>[Envanter]</color> Ekleme: <b>{item.ItemName}</b> (x{amount}). Hedef: Slot {activeSlotIndex}");
        int remainingAmount = amount;

        // 1. ÖNCELİK: Seçili slot boşsa veya aynı eşyadan varsa orayı doldur (Ele gelsin diye)
        if (Slots[activeSlotIndex].IsEmpty || (Slots[activeSlotIndex].Item == item && Slots[activeSlotIndex].Amount < item.MaxStack))
        {
            int space = item.MaxStack - Slots[activeSlotIndex].Amount;
            int amountToAdd = Mathf.Min(space, remainingAmount);

            if (Slots[activeSlotIndex].IsEmpty)
                Slots[activeSlotIndex] = new InventorySlot(item, amountToAdd);
            else
                Slots[activeSlotIndex].Amount += amountToAdd;

            remainingAmount -= amountToAdd;
            UpdateSlotClientRpc(activeSlotIndex, item.ItemID, Slots[activeSlotIndex].Amount);
            if (remainingAmount <= 0) return;
        }

        // 2. DİĞER VAR OLAN STACKLERİ DOLDUR
        for (int i = 0; i < Slots.Length; i++)
        {
            if (i == activeSlotIndex) continue;
            if (!Slots[i].IsEmpty && Slots[i].Item == item && Slots[i].Amount < item.MaxStack)
            {
                int space = item.MaxStack - Slots[i].Amount;
                int amountToAdd = Mathf.Min(space, remainingAmount);
                Slots[i].Amount += amountToAdd;
                remainingAmount -= amountToAdd;
                UpdateSlotClientRpc(i, item.ItemID, Slots[i].Amount);
                if (remainingAmount <= 0) return;
            }
        }

        // 3. DİĞER BOŞ SLOTLARA EKLE
        for (int i = 0; i < Slots.Length; i++)
        {
            if (i == activeSlotIndex) continue;
            if (Slots[i].IsEmpty)
            {
                int amountToAdd = Mathf.Min(item.MaxStack, remainingAmount);
                Slots[i] = new InventorySlot(item, amountToAdd);
                remainingAmount -= amountToAdd;
                UpdateSlotClientRpc(i, item.ItemID, Slots[i].Amount);
                if (remainingAmount <= 0) return;
            }
        }
    }

    // RemoveItemServer metodunu bu parametrelerle güncelle
    [ServerRpc]
    public void RemoveItemServerRpc(int index, int amountToRemove, Vector3 spawnPos, Vector3 throwDir, bool spawnVisual = true)
    {
        if (!IsServer) return;

        if (index >= 0 && index < Slots.Length && !Slots[index].IsEmpty)
        {
            ItemData itemToDrop = Slots[index].Item;
            Slots[index].Amount -= amountToRemove;
            if (Slots[index].Amount <= 0) Slots[index].Clear();

            // Sadece fırlatma istendiğinde görsel oluştur (G tuşu gibi)
            if (spawnVisual && itemToDrop != null && itemToDrop.DropPrefab != null)
            {
                GameObject droppedObj = Instantiate(itemToDrop.DropPrefab, spawnPos, Quaternion.LookRotation(throwDir));
                if (droppedObj.TryGetComponent(out NetworkObject netObj)) netObj.Spawn();

                // Oyuncuyla çakışmayı engelle
                Collider playerCollider = GetComponent<Collider>();
                Collider itemCollider = droppedObj.GetComponent<Collider>();
                if (playerCollider != null && itemCollider != null) Physics.IgnoreCollision(playerCollider, itemCollider);

                if (droppedObj.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(throwDir * 7f, ForceMode.Impulse);
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 3f, ForceMode.Impulse);
                }
            }

            string itemID = Slots[index].IsEmpty ? "" : itemToDrop.ItemID;
            UpdateSlotClientRpc(index, itemID, Slots[index].Amount);
        }
    }

    [ClientRpc]
    private void UpdateSlotClientRpc(int index, string itemID, int newAmount)
    {
        ItemData itemData = string.IsNullOrEmpty(itemID) ? null : ItemDatabase.Instance.GetItemByID(itemID);
        Slots[index] = new InventorySlot(itemData, newAmount);
        if (Slots[index].Amount <= 0) Slots[index].Clear();
        OnSlotUpdated?.Invoke(index, Slots[index]);
    }
}