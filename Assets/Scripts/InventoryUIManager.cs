using UnityEngine;
using UnityEngine.UI;

public class InventoryUIManager : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public Image[] slotIcons; // 10 adet Hotbar ikonu
    public Text[] slotAmountTexts; // 10 adet miktar metni

    private void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.OnSlotChanged += UpdateUI;
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.OnSlotChanged -= UpdateUI;
        }
    }

    private void UpdateUI(int index, InventorySlot slot)
    {
        // Sadece ilk 10 slotu (Hotbar) UI'da gösteriyoruz
        if (index >= slotIcons.Length) return;

        if (slot.IsEmpty)
        {
            slotIcons[index].sprite = null;
            slotIcons[index].enabled = false;
            slotAmountTexts[index].text = "";
        }
        else
        {
            slotIcons[index].sprite = slot.itemData.icon;
            slotIcons[index].enabled = true;
            slotAmountTexts[index].text = slot.amount > 1 ? slot.amount.ToString() : "";
        }
    }
}