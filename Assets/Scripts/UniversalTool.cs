using UnityEngine;
using UnityEngine.InputSystem;

public class UniversalTool : MonoBehaviour
{
    public float interactionDistance = 3f;
    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Update()
    {
        if (inventory != null && !inventory.IsOwner) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
            {
                // Vurduðumuz obje Interface kuralýna uyuyor mu? (Ýnek veya Þiþe)
                if (hit.collider.TryGetComponent(out IInteractableTarget target))
                {
                    int activeSlot = inventory.activeHotbarIndex.Value;
                    if (inventory.slots[activeSlot].itemData != null)
                    {
                        int currentID = inventory.slots[activeSlot].itemData.itemID;
                        // Hedefe envanterimizi ve eþya ID'mizi gönderiyoruz
                        target.OnInteract(inventory, activeSlot, currentID);
                    }
                }
            }
        }
    }
}