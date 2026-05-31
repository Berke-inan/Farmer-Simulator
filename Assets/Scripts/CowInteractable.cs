using UnityEngine;
using Unity.Netcode;

public class CowInteractable : NetworkBehaviour, IInteractable
{
    public int bosKovaID = 11;
    public int doluKovaID = 12;

    public NetworkVariable<bool> gunlukSagildiMi = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void Interact(NetworkObject interactor)
    {
        if (gunlukSagildiMi.Value)
        {
            Debug.Log("Ýnek bugün zaten saðýldý! Yarýna kadar beklemen lazým.");
            return;
        }

        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        int activeIndex = inventory.activeHotbarIndex.Value;
        InventorySlot activeSlot = inventory.slots[activeIndex];

        if (!activeSlot.IsEmpty && activeSlot.itemData != null && activeSlot.itemData.itemID == bosKovaID)
        {
            SutSagServerRpc(interactor.NetworkObjectId, activeIndex);
        }
        else
        {
            Debug.Log("Süt saðmak için eline 'Boþ Kova' almalýsýn!");
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SutSagServerRpc(ulong playerId, int slotIndex)
    {
        if (gunlukSagildiMi.Value) return;

        gunlukSagildiMi.Value = true; // Günde 1 kez mantýðý
        SutSagClientRpc(playerId, slotIndex);
    }

    [Rpc(SendTo.Everyone)]
    private void SutSagClientRpc(ulong playerId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            if (playerObj.IsOwner)
            {
                PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
                inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
                inventory.GiveSpecificItemServerRpc(doluKovaID, 1);
                Debug.Log("Baþarýlý: Ýnek saðýldý, eline Dolu Kova geldi!");
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void YeniGunBasladiServerRpc()
    {
        gunlukSagildiMi.Value = false; // Sabah olunca ineði tekrar saðmaya açar
    }
}