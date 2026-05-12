using Unity.Netcode;
using UnityEngine;

public class OrakEylemi : MonoBehaviour, IUseableTool
{
    public float yaricap = 2.5f;

    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        if (inv.TryGetComponent(out PlayerActionManager actionManager))
        {
            // 1. Ekinleri Hasat Et (Mevcut kod)
            Collider[] cols = Physics.OverlapSphere(hit.point, yaricap);
            foreach (var c in cols)
            {
                if (c.TryGetComponent(out ModularCrop ekin) && (ekin.IsGrown || ekin.IsRotted))
                {
                    if (ekin.TryGetComponent(out NetworkObject n))
                    {
                        actionManager.HasatEtServerRpc(n.NetworkObjectId, ekin.transform.position, ekin.IsGrown, ekin.extraYield.Value);
                    }
                }
            }

            // 2. YENİ: Terrain detaylarını (otları) sil
            // Burayı çağırdığında vurduğun yerin etrafındaki otlar ağda silinecek
            actionManager.RemoveTerrainDetailsServerRpc(hit.point, yaricap);
        }
    }
}