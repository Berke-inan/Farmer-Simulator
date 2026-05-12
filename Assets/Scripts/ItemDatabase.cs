using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Tooltip("Tüm ScriptableObject ItemData'ları buraya eklenmeli.")]
    public List<ItemData> AllItems;

    private Dictionary<string, ItemData> itemDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Sahne geçişlerinde silinmemesi için
            InitializeDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDatabase()
    {
        itemDictionary = new Dictionary<string, ItemData>();
        foreach (var item in AllItems)
        {
            if (item != null && !itemDictionary.ContainsKey(item.ItemID))
            {
                itemDictionary.Add(item.ItemID, item);
            }
        }
    }

    public ItemData GetItemByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        itemDictionary.TryGetValue(id, out ItemData item);
        return item;
    }
}