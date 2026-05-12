using System;

[Serializable]
public struct InventorySlot
{
    public ItemData Item;
    public int Amount;

    public bool IsEmpty => Item == null || Amount <= 0;

    public InventorySlot(ItemData item, int amount)
    {
        Item = item;
        Amount = amount;
    }

    public void Clear()
    {
        Item = null;
        Amount = 0;
    }
}