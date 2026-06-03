using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class LaptopSellStation : NetworkBehaviour, IInteractable
{
    public void Interact(NetworkObject playerNetworkObject)
    {
        if (!playerNetworkObject.IsOwner) return;

        PlayerInventory inventory = playerNetworkObject.GetComponent<PlayerInventory>();
        int activeIdx = inventory.activeHotbarIndex.Value;
        InventorySlot activeSlot = inventory.slots[activeIdx];

        // Slot boş değilse satma işlemini başlat
        if (!activeSlot.IsEmpty)
        {
            SellItemServerRpc(playerNetworkObject.NetworkObjectId, activeIdx);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SellItemServerRpc(ulong playerNetworkId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerObj))
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            InventorySlot slot = inventory.slots[slotIndex];

            if (!slot.IsEmpty)
            {
                // Parayı ekle (EconomyManager scriptinin var olduğu varsayılır)
                EconomyManager.Instance.AddMoney(slot.itemData.price);

                // Envanterden eşyayı tamamen sil (tüm miktarını düşür)
                inventory.DecreaseItemAmountServerRpc(slotIndex, slot.amount);

                Debug.Log($"{slot.itemData.itemName} satıldı!");
            }
        }
    }

    // Satış noktasına bakıldığında HUD'da görünecek yönerge
    public List<ActionPrompt> GetPrompts()
    {
        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", "SAT")
        };
    }
}