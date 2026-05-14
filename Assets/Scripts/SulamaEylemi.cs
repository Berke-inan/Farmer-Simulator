using UnityEngine;

public class SulamaEylemi : MonoBehaviour, IUseableTool
{
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            if (manager.IsSoilTilled(hit.point))
            {
                manager.PaintSoilServerRpc(hit.point, manager.wetLayerIndex, fircaBoyutu);
            }
        }
    }
}