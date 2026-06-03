using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class InteractableItem : NetworkBehaviour, IInteractable
{
    [Header("Item Identifier")]
    public int itemID;

    [Header("Amount")]
    public int amount = 1;

    public void Interact(NetworkObject playerNetworkObject)
    {
        if (playerNetworkObject.TryGetComponent(out PlayerInventory inventory))
        {
            inventory.RequestPickupServerRpc(NetworkObjectId);
            Debug.Log($"[Sistem] {itemID} ID'li eşya için toplama isteği gönderildi.");
        }
    }

    // Yerdeki eşyaya bakıldığında HUD'da görünecek yönerge
    public List<ActionPrompt> GetPrompts()
    {
        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", "Etkileşime Geç")
        };
    }
}