using UnityEngine;
using Unity.Netcode;

public class FertilizerItem : MonoBehaviour, IUseableTool
{
    [Header("Fertilizer Properties")]
    public float growthTimeMultiplier = 0.5f;
    public int yieldBonus = 1;

    // Parametre tipini PlayerInventory olarak güncelledik.
    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // 1. Işının çarptığı objede ModularCrop ara
        ModularCrop hedefEkin = hit.collider.GetComponentInParent<ModularCrop>();

        if (hedefEkin != null && !hedefEkin.isFertilized.Value)
        {
            if (hedefEkin.TryGetComponent(out NetworkObject n))
            {
                // 2. PlayerActionManager'ı bulmaya çalışıyoruz
                if (inventory.TryGetComponent(out PlayerActionManager actionManager))
                {
                    // 3. Yeni sistemde hotbar indeksi doğrudan inventory içinde duruyor
                    int slotIndex = inventory.activeHotbarIndex.Value;

                    // RPC komutunu gönderiyoruz
                    actionManager.GubreleServerRpc(
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
}