using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketItem", menuName = "Market/Item")]
public class MarketItem : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public int itemID;
    public string itemName;
    public Sprite itemIcon;
    public int price;
    public GameObject prefabToSpawn;

    [Header("Stok Ayarları")]
    [Tooltip("Eğer true ise, stok sınırı otomatik 1 olarak kabul edilir (Örn: Jeneratör).")]
    public bool isMachine;

    [Tooltip("Makineler dışındaki eşyaların maksimum stoğu.")]
    public int maxStock = 10;

    [Tooltip("Eksilen stoğun 1 adet yenilenmesi için gereken süre (Saniye).")]
    public float restockInterval = 60f;
}