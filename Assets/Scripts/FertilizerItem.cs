using UnityEngine;
using Unity.Netcode;

public class FertilizerItem : MonoBehaviour, IUseableTool
{
    [Header("Fertilizer Properties")]
    public float growthTimeMultiplier = 0.5f;
    public int yieldBonus = 1;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // 1. Işının çarptığı objede ModularCrop ara
        ModularCrop hedefEkin = hit.collider.GetComponentInParent<ModularCrop>();

        if (hedefEkin != null && !hedefEkin.isFertilized.Value)
        {
            if (hedefEkin.TryGetComponent(out NetworkObject n))
            {
                // 2. Yeni sistemde hotbar indeksi ve metodlar doğrudan inventory içinde
                int slotIndex = inventory.activeHotbarIndex.Value;

                // RPC komutunu gönderiyoruz
                inventory.GubreleServerRpc(
                    n.NetworkObjectId,
                    growthTimeMultiplier,
                    yieldBonus,
                    slotIndex
                );

                Debug.Log("Gübreleme komutu gönderildi!");
            }
        }
    }
}