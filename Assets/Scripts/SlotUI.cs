using UnityEngine;
using UnityEngine.UI;
using TMPro; // Eğer TextMeshPro kullanıyorsan

public class SlotUI : MonoBehaviour
{
    [Header("Görsel Bileşenler")]
    public Image iconImage;
    public TextMeshProUGUI amountText; // Standart Text kullanıyorsan 'Text' olarak değiştir

    // Bu metot InventoryUIManager tarafından çağrılacak
    public void SetSlot(InventorySlot slot)
    {
        // Eski kodda muhtemelen 'slot.Item' yazıyordu, burayı 'itemData' yaptık
        if (slot == null || slot.IsEmpty)
        {
            ClearSlot();
            return;
        }

        // İkonu ayarla
        iconImage.sprite = slot.itemData.icon;
        iconImage.enabled = true;

        // Miktarı ayarla (1'den büyükse göster)
        if (slot.amount > 1)
        {
            amountText.text = slot.amount.ToString();
            amountText.enabled = true;
        }
        else
        {
            amountText.enabled = false;
        }
    }

    public void ClearSlot()
    {
        iconImage.sprite = null;
        iconImage.enabled = false;
        amountText.enabled = false;
    }
}