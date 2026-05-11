using Unity.Netcode;
using UnityEngine;

// IUseableTool arayüzü eklendi
public class FertilizerItem : NetworkBehaviour, IUseableTool
{
    [Header("Fertilizer Properties")]
    public float growthTimeMultiplier = 0.5f;
    public int yieldBonus = 1;

    // PlayerInteractor tarafından çağrılacak fonksiyon
    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        // Işının çarptığı objede veya ebeveyninde ModularCrop var mı kontrolü
        ModularCrop hedefEkin = hit.collider.GetComponent<ModularCrop>();
        if (hedefEkin == null)
        {
            hedefEkin = hit.collider.GetComponentInParent<ModularCrop>();
        }

        // Ekin bulunduysa ve henüz gübrelenmemişse
        if (hedefEkin != null && !hedefEkin.isFertilized.Value)
        {
            if (hedefEkin.TryGetComponent(out NetworkObject n))
            {
                // Yetki sunucuda olduğu için RPC ile işlem başlatılır
                ApplyFertilizerServerRpc(n.NetworkObjectId);

                // NOT: Eğer PlayerInventory scriptinde eldeki eşyayı silmek 
                // için özel bir fonksiyonun varsa (Örn: inv.EldekiniYokEt()) 
                // onu burada çağırmalısın. Aksi takdirde obje yok olsa bile 
                // envanter sistemi elinde hala bir şey var sanabilir.
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ApplyFertilizerServerRpc(ulong cropNetworkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropNetworkObjectId, out NetworkObject cropObj))
        {
            if (cropObj.TryGetComponent(out ModularCrop targetCrop))
            {
                // Çift kontrol: Ekin hala gübrelenmemiş mi?
                if (!targetCrop.isFertilized.Value)
                {
                    targetCrop.ApplyFertilizer(growthTimeMultiplier, yieldBonus);

                    // Gübre kullanıldıktan sonra objeyi sunucudan (ve oyunculardan) sil
                    if (NetworkObject != null && NetworkObject.IsSpawned)
                    {
                        NetworkObject.Despawn(true);
                    }
                    else
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }
    }
}
