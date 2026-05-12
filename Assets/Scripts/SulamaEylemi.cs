using UnityEngine;

public class SulamaEylemi : MonoBehaviour, IUseableTool
{

    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            // Sadece çapalanmış yerler sulanabilir
            if (manager.IsSoilTilled(hit.point))
            {
                
                manager.PaintSoilServerRpc(hit.point, manager.wetLayerIndex,fircaBoyutu);
            }
        }
    }
}