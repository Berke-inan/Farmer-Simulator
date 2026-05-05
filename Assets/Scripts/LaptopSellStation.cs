using Unity.Netcode;
using UnityEngine;

public class LaptopSellStation : NetworkBehaviour, IInteractable
{
    public void Interact(NetworkObject playerNetworkObject)
    {
        if (!playerNetworkObject.IsOwner) return;

        PlayerInventory inventory = playerNetworkObject.GetComponent<PlayerInventory>();

        if (inventory != null && inventory.eldekiObje != null)
        {
            if (inventory.eldekiObje.TryGetComponent<SellableItem>(out SellableItem itemToSell))
            {
                ulong itemNetworkId = inventory.eldekiObje.GetComponent<NetworkObject>().NetworkObjectId;
                ulong playerNetworkId = playerNetworkObject.NetworkObjectId;

                SellItemServerRpc(itemNetworkId, playerNetworkId);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SellItemServerRpc(ulong itemNetworkId, ulong playerNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj))
        {
            SellableItem itemToSell = itemObj.GetComponent<SellableItem>();

            if (itemToSell != null)
            {
                EconomyManager.Instance.AddMoney(itemToSell.price);

                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerObj))
                {
                    PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
                    if (inventory != null)
                    {
                        inventory.EnvanteriTemizleServerRpc();
                    }
                }

                itemObj.Despawn();
            }
        }
    }
}