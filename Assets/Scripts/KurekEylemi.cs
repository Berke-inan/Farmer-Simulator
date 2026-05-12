using UnityEngine;

public class KurekEylemi : MonoBehaviour, IUseableTool
{

    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;
    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            tCol.GetComponent<TerrainLayerManager>().PaintSoilServerRpc(hit.point, 0, fircaBoyutu);
        }
    }
}