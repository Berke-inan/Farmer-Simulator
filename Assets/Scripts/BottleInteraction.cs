using UnityEngine;
using Unity.Netcode;

public class BottleInteraction : NetworkBehaviour, IInteractableTarget
{
    private InteractableItem interactableItem; // Yerden alma kodun

    private void Awake()
    {
        interactableItem = GetComponent<InteractableItem>();
    }

    // Biri bize aletle vurduðunda
    public void OnInteract(PlayerInventory inventory, int activeSlotIndex, int currentItemID)
    {
        // Vuran eþya 8 (Dolu Kova) ise ve þiþe boþsa (ID: 9)
        if (interactableItem != null && interactableItem.itemID == 9 && currentItemID == 8)
        {
            FillBottleRpc(inventory.NetworkObjectId, activeSlotIndex);
        }
    }

    [Rpc(SendTo.Server)]
    private void FillBottleRpc(ulong playerNetworkId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerObj))
        {
            if (playerObj.TryGetComponent(out PlayerInventory inventory))
            {
                // Þiþenin kendi ID'sini 10 (Dolu Þiþe) yap ki oyuncu E'ye basýnca dolu þiþe alabilsin
                interactableItem.itemID = 10;

                // Oyuncunun 8'ini (Dolu Kova) sil, 7'yi (Boþ Kova) geri ver!
                inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
                inventory.GiveSpecificItemServerRpc(7, 1, new RpcParams { Receive = new RpcReceiveParams { SenderClientId = playerObj.OwnerClientId } });
            }
        }
    }
}