public interface IInteractableTarget
{
    // Aletle vurulduðunda çalýþacak ortak kural
    void OnInteract(PlayerInventory inventory, int activeSlotIndex, int currentItemID);
}