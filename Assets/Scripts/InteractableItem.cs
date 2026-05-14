using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
// BURAYA DİKKAT: IInteractable arayüzünü ekledik
public class InteractableItem : NetworkBehaviour, IInteractable
{
    [Header("Item Identifier")]
    public int itemID;

    [Header("Amount")]
    public int amount = 1;

    // PlayerInteractor "E"ye bastığında burayı tetikleyecek
    public void Interact(NetworkObject playerNetworkObject)
    {
        // Komutu gönderen oyuncunun envanter sistemine ulaşıyoruz
        if (playerNetworkObject.TryGetComponent(out PlayerInventory inventory))
        {
            // Envantere "bu objeyi (NetworkObjectId) yerden al" emri gönderiyoruz
            inventory.RequestPickupServerRpc(NetworkObjectId);

            Debug.Log($"[Sistem] {itemID} ID'li eşya için toplama isteği gönderildi.");
        }
    }
}