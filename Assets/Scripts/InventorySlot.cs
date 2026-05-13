using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    public InventorySlot()
    {
        itemData = null;
        amount = 0;
    }

    public bool IsEmpty => itemData == null || amount <= 0;

    public void AddItem(ItemData data, int count)
    {
        itemData = data;
        amount += count;
    }

    public void ClearSlot()
    {
        itemData = null;
        amount = 0;
    }
}