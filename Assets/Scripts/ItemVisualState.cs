using UnityEngine;

public class ItemVisualState : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    [Tooltip("Ýçindeki beyaz süt silindiri")]
    public GameObject liquidMesh;

    [Tooltip("Hangi ID'lerde bu süt açýk olmalý? (Kova için 8, Þiþe için 10 yaz)")]
    public int fullItemID;

    // Eldeki objeler için Update içinde envanteri dinler
    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();

        // Eðer yerdeki bir objeyse (envanteri yoksa), kendi InteractableItem ID'sine bakar
        if (inventory == null && TryGetComponent(out InteractableItem groundItem))
        {
            SetVisual(groundItem.itemID);
        }
    }

    private void Update()
    {
        // Eðer eldeki bir objeyse, aktif slotun ID'sini anlýk takip eder
        if (inventory != null && inventory.slots.Length > 0)
        {
            int slot = inventory.activeHotbarIndex.Value;
            if (inventory.slots[slot].itemData != null)
            {
                SetVisual(inventory.slots[slot].itemData.itemID);
            }
        }
    }

    private void SetVisual(int currentID)
    {
        if (liquidMesh != null)
        {
            liquidMesh.SetActive(currentID == fullItemID);
        }
    }
}