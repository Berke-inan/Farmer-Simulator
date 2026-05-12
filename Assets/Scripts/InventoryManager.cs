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
        for (int i = 0; i < Slots.Length; i++) Slots[i] = new InventorySlot(null, 0);
    }

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
        int remainingAmount = amount;

        // 1. ÖNCELİK: Aktif slot
        if (Slots[activeSlotIndex].IsEmpty || (Slots[activeSlotIndex].Item == item && Slots[activeSlotIndex].Amount < item.MaxStack))
        {
            int space = item.MaxStack - Slots[activeSlotIndex].Amount;
            int amountToAdd = Mathf.Min(space, remainingAmount);
            if (Slots[activeSlotIndex].IsEmpty) Slots[activeSlotIndex] = new InventorySlot(item, amountToAdd);
            else Slots[activeSlotIndex].Amount += amountToAdd;
            remainingAmount -= amountToAdd;
            UpdateSlotClientRpc(activeSlotIndex, item.ItemID, Slots[activeSlotIndex].Amount);
            if (remainingAmount <= 0) return;
        }

        // 2. DİĞER STACKLER VE BOŞ SLOTLAR (Loop basitleştirildi)
        for (int i = 0; i < Slots.Length; i++)
        {
            if (i == activeSlotIndex || remainingAmount <= 0) continue;
            // Buraya normal ekleme mantığı gelir...
            // (Yukarıdaki AddItemServer kodunun devamını buraya yapıştırabilirsin)
        }
    }

    [ServerRpc]
    public void RemoveItemServerRpc(int index, int amountToRemove, Vector3 spawnPos, Vector3 throwDir, bool spawnVisual = true)
    {
        if (!IsServer) return;

        if (index >= 0 && index < Slots.Length && !Slots[index].IsEmpty)
        {
            ItemData itemToDrop = Slots[index].Item;
            Slots[index].Amount -= amountToRemove;
            if (Slots[index].Amount <= 0) Slots[index].Clear();

            if (spawnVisual && itemToDrop != null && itemToDrop.DropPrefab != null)
            {
                GameObject droppedObj = Instantiate(itemToDrop.DropPrefab, spawnPos, Quaternion.LookRotation(throwDir));
                if (droppedObj.TryGetComponent(out NetworkObject netObj)) netObj.Spawn();

                // FİZİK ÇAKIŞMA ENGELLEYİCİ
                Collider playerCol = GetComponent<Collider>();
                Collider itemCol = droppedObj.GetComponent<Collider>();
                if (playerCol != null && itemCol != null) Physics.IgnoreCollision(playerCol, itemCol);

                if (droppedObj.TryGetComponent(out Rigidbody rb))
                {
                    rb.AddForce(throwDir * 7f, ForceMode.Impulse);
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 3f, ForceMode.Impulse);
                }
            }
            UpdateSlotClientRpc(index, Slots[index].IsEmpty ? "" : itemToDrop.ItemID, Slots[index].Amount);
        }
    }

    [ClientRpc]
    private void UpdateSlotClientRpc(int index, string itemID, int newAmount)
    {
        // EĞER itemID boş gelirse slotu temizle, dolu gelirse Database'den çek
        ItemData itemData = string.IsNullOrEmpty(itemID) ? null : ItemDatabase.Instance.GetItemByID(itemID);

        // DEBUG: Eğer itemID var ama itemData null dönüyorsa Database'de ID hatası vardır
        if (!string.IsNullOrEmpty(itemID) && itemData == null)
        {
            Debug.LogError($"<color=red>[HATA]</color> ItemDatabase '{itemID}' ID'sini bulamadı! Eşya bu yüzden kayboluyor.");
        }

        Slots[index] = new InventorySlot(itemData, newAmount);

        if (Slots[index].Amount <= 0) Slots[index].Clear();

        OnSlotUpdated?.Invoke(index, Slots[index]);
    }
}