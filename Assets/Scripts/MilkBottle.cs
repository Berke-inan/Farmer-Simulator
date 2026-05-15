using UnityEngine;
using Unity.Netcode;

public class MilkBottle : NetworkBehaviour, IInteractableTarget
{
    public GameObject milkLiquidVisual; // Ýçindeki süt görseli
    public NetworkVariable<bool> isBottleFull = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int emptyBucketID = 7;
    public int fullBucketID = 8;

    public override void OnNetworkSpawn()
    {
        isBottleFull.OnValueChanged += (oldVal, newVal) =>
        {
            if (milkLiquidVisual != null) milkLiquidVisual.SetActive(newVal);
        };
        if (milkLiquidVisual != null) milkLiquidVisual.SetActive(isBottleFull.Value);
    }

    // Aletle vurulduðunda çalýþýr
    public void OnInteract(PlayerInventory inventory, int activeSlotIndex, int currentItemID)
    {
        // Bana vuran eþya 8 numaralý dolu kova ise ve ben boþsam:
        if (!isBottleFull.Value && currentItemID == fullBucketID)
        {
            FillBottleRpc(inventory.NetworkObjectId, activeSlotIndex);
        }
    }

    [Rpc(SendTo.Server)]
    public void FillBottleRpc(ulong playerNetworkId, int slotIndex)
    {
        if (!isBottleFull.Value)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerObj))
            {
                if (playerObj.TryGetComponent(out PlayerInventory inventory))
                {
                    isBottleFull.Value = true; // Þiþe doldu

                    // 8 ID'li eþyayý sil, 7 ID'li eþyayý (Boþ Kova) geri ver
                    inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
                    inventory.GiveSpecificItemServerRpc(emptyBucketID, 1, new RpcParams { Receive = new RpcReceiveParams { SenderClientId = playerObj.OwnerClientId } });
                }
            }
        }
    }
}