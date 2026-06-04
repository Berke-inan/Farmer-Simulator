using System;

[Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int amount;

    // Aletlerin kýrýlma/dayanýklýlýk caný
    public float kalanCan;

    // YENÝ: Tohum paketi vb. eþyalarýn içindeki kullaným hakký
    public int kalanEkimHakki;

    public InventorySlot()
    {
        itemData = null;
        amount = 0;
        kalanCan = -1f;
        kalanEkimHakki = -1; // -1 demek = henüz hiç kullanýlmamýþ taze paket
    }

    public bool IsEmpty => itemData == null || amount <= 0;

    // Eþya eklenirken her iki deðeri de alabilecek þekilde güncelledik
    public void AddItem(ItemData data, int count, float can = -1f, int ekimHakki = -1)
    {
        itemData = data;
        amount += count;
        kalanCan = can;
        kalanEkimHakki = ekimHakki;
    }

    public void ClearSlot()
    {
        itemData = null;
        amount = 0;
        kalanCan = -1f;
        kalanEkimHakki = -1;
    }
}