using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    public void UpdateUI(InventorySlot slot)
    {
        if (slot.IsEmpty)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            amountText.text = "";
        }
        else
        {
            iconImage.sprite = slot.Item.Icon;
            iconImage.enabled = true;
            amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : "";
        }
    }
}