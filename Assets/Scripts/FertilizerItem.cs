using UnityEngine;
using Unity.Netcode;

public class FertilizerItem : MonoBehaviour, IUseableTool
{
    [Header("Fertilizer Properties")]
    public float growthTimeMultiplier = 0.5f;
    public int yieldBonus = 1;

    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        // Işının çarptığı objede ModularCrop ara
        ModularCrop hedefEkin = hit.collider.GetComponentInParent<ModularCrop>();

        if (hedefEkin != null && !hedefEkin.isFertilized.Value)
        {
            if (hedefEkin.TryGetComponent(out NetworkObject n))
            {
                // Komutu oyuncunun kendi merkezindeki PlayerActionManager'a gönderiyoruz
                if (inv.TryGetComponent(out PlayerActionManager actionManager) && inv.TryGetComponent(out NetworkedHotbar hotbar))
                {
                    int slotIndex = hotbar.ActiveSlotIndex.Value;

                    // Yeni Yer: PlayerActionManager üzerindeki RPC
                    actionManager.GubreleServerRpc(
                        n.NetworkObjectId,
                        growthTimeMultiplier,
                        yieldBonus,
                        slotIndex
                    );
                }
            }
        }
    }
}