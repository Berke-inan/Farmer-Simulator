using UnityEngine;

public enum ItemType
{
    Seed,
    Tool,
    Crop,
    Material
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public int itemID;
    public string itemName;
    public ItemType itemType;
    public int maxStack = 64;
    public int price;

    [Header("UI Visuals")]
    public Sprite icon;

    [Header("In-Game Visuals")]
    public GameObject heldModelPrefab;
    public GameObject groundPrefab;
    public Vector3 holdPositionOffset;
    public Vector3 holdRotationOffset;

    [Header("Tarım Ayarları")]
    public GameObject ekinPrefab;
    public int maxEkimHakki = 5;
}