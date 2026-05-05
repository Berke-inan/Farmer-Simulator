using UnityEngine;

[CreateAssetMenu(fileName = "NewMarketItem", menuName = "Market/Item")]
public class MarketItem : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite itemIcon;
    public int price;
    public GameObject prefabToSpawn;
}