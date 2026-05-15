using UnityEngine;
using Unity.Netcode;

public class CowInteraction : NetworkBehaviour, IInteractableTarget
{
    public NetworkVariable<bool> hasMilk = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Biri bize aletle vurduðunda otomatik çalýþýr
    public void OnInteract(PlayerInventory inventory, int activeSlotIndex, int currentItemID)
    {
        // Vuran eþya 7 (Boþ Kova) ise ve sütümüz varsa
        if (hasMilk.Value && currentItemID == 7)
        {
            RequestMilkRpc(inventory.NetworkObjectId, activeSlotIndex);
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestMilkRpc(ulong playerNetworkId, int slotIndex)
    {
        if (hasMilk.Value && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerObj))
        {
            if (playerObj.TryGetComponent(out PlayerInventory inventory))
            {
                hasMilk.Value = false;

                // 7'yi sil, yerine 8 ver!
                inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
                inventory.GiveSpecificItemServerRpc(8, 1, new RpcParams { Receive = new RpcReceiveParams { SenderClientId = playerObj.OwnerClientId } });
            }
        }
    }
}