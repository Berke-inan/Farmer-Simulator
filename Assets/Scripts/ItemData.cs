using UnityEngine;

// Eşya türlerini belirleyen Enum
public enum ItemType
{
    Seed,
    Tool,
    Crop,
    Material,
    Consumable,
    Bale
}

[CreateAssetMenu(fileName = "NewItemData", menuName = "FarmerSim/Inventory/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Temel Veriler")]
    public string ItemID;        // Ağ senkronizasyonu ve Database araması için benzersiz ID
    public string ItemName;      // UI'da görünecek isim
    public ItemType Type;        // Eşyanın türü

    [Header("Görsel Ayarlar")]
    public Sprite Icon;          // Envanter slotundaki resim

    [Tooltip("Elde tutulurken görünecek hafif model (NetworkObject/Rigidbody OLMAMALI)")]
    public GameObject EquipPrefab;

    [Tooltip("Yere atıldığında görünecek fiziksel model (NetworkObject/Rigidbody OLMALI)")]
    public GameObject DropPrefab;

    [Header("Envanter Ayarları")]
    public int MaxStack = 64;    // Bir slotta en fazla kaç adet birikebilir?

    [Header("Tohum ve Makine Ayarları")]
    [Tooltip("Sadece Tohum türündeki eşyalar için doldurulmalıdır.")]
    public GameObject EkinPrefab; // Toprakta doğacak bitki modeli
    public int TohumID;           // Bitkinin büyüme aşamalarını takip etmek için kullanılan ID
}