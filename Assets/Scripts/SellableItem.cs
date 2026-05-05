using Unity.Netcode;
using UnityEngine;

public class SellableItem : NetworkBehaviour
{
    // MarketItem scriptable object dosyasını buraya sürükleyeceğiz (Resim ve isim için)
    public MarketItem itemData;

    // Eğer özel bir fiyat vermek istersen burayı kullanabilirsin, 
    // yoksa market verisindeki fiyatı kullanırız.
    public int price = 0;
    public int GetPrice() => price > 0 ? price : (itemData != null ? itemData.price : 0);
}