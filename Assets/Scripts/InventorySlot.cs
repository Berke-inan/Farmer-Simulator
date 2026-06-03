using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    // YENÝ EKLENDÝ: Eþyanýn canýný hatýrlayan deðiþken (-1 demek caný full/sýfýr eþya demek)
    public float kalanCan;

    public InventorySlot()
    {
        itemData = null;
        amount = 0;
        kalanCan = -1f;
    }

    public bool IsEmpty => itemData == null || amount <= 0;

    // YENÝ EKLENDÝ: AddItem fonksiyonu artýk can bilgisini de alýyor
    public void AddItem(ItemData data, int count, float can = -1f)
    {
        itemData = data;
        amount += count;
        kalanCan = can;
    }

    public void ClearSlot()
    {
        itemData = null;
        amount = 0;
        kalanCan = -1f;
    }
}