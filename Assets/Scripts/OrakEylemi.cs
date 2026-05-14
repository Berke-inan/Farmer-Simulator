using UnityEngine;
using Unity.Netcode;

public class OrakEylemi : MonoBehaviour, IUseableTool
{
    public float yaricap = 2.5f;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (inventory.TryGetComponent(out PlayerActionManager actionManager))
        {
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
            actionManager.RemoveTerrainDetailsServerRpc(hit.point, yaricap);
        }
    }
}