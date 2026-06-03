using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class InteractableItem : NetworkBehaviour, IInteractable
{
    [Header("Item Identifier")]
    public int itemID;

    [Header("Amount")]
    public int amount = 1;

    [Header("Hafıza: Kalan Can")]
    // YENİ EKLENDİ: Yere düştüğü an bu değer dolacak ve başkası alana kadar bekleyecek
    public float kalanCan = -1f;

    public void Interact(NetworkObject playerNetworkObject)
    {
        if (playerNetworkObject.TryGetComponent(out PlayerInventory inventory))
        {
            inventory.RequestPickupServerRpc(NetworkObjectId);
            Debug.Log($"[Sistem] {itemID} ID'li eşya için toplama isteği gönderildi.");
        }
    }
    public List<ActionPrompt> GetPrompts()
    {
        return new List<ActionPrompt>
    {
        new ActionPrompt("E", "Eşyayı Yerden Al")
    };
    }
}