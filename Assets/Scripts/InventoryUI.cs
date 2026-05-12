using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    // Bu script oyuncunun UI canvasında bulunur.
    [SerializeField] private InventoryManager targetInventory;
    [SerializeField] private SlotUI[] uiSlots;

    private void OnEnable()
    {
        if (targetInventory != null)
            targetInventory.OnSlotUpdated += UpdateSlotVisual;
    }

    private void OnDisable()
    {
        if (targetInventory != null)
            targetInventory.OnSlotUpdated -= UpdateSlotVisual;
    }

    private void UpdateSlotVisual(int slotIndex, InventorySlot slotData)
    {
        if (slotIndex >= 0 && slotIndex < uiSlots.Length)
        {
            uiSlots[slotIndex].UpdateUI(slotData);
        }
    }
}