using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class CubeInteractable : NetworkBehaviour, IInteractable
{
    [Header("Eşya Ayarları")]
    public int verilecekItemID = 101;
    public int miktar = 1;

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            GiveItemServerRpc(interactor.NetworkObjectId);
        }
    }

    // Küpe bakıldığında HUD'da görünecek yönerge
    public List<ActionPrompt> GetPrompts()
    {
        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", "TOPLA")
        };
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void GiveItemServerRpc(ulong oyuncuID)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuID, out NetworkObject oyuncuNetObj))
        {
            PlayerInventory inventory = oyuncuNetObj.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.GiveSpecificItemServerRpc(verilecekItemID, miktar);
            }
        }
    }
}