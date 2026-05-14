using UnityEngine;

public class KurekEylemi : MonoBehaviour, IUseableTool
{
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    // parametre tipi inventorymanager yerine playerinventory olarak guncellendi
    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (hit.collider != null && hit.collider is TerrainCollider tCol)
        {
            // terrainlayermanager uzerindeki toprak boyama rpc sini tetikler
            tCol.GetComponent<TerrainLayerManager>().PaintSoilServerRpc(hit.point, 0, fircaBoyutu);
            Debug.Log("kurek ile toprak duzeltildi");
        }
    }
}