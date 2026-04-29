using UnityEngine;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        // Konsola neye vurduğumuzu yazdıralım ki bilelim
        Debug.Log("Çapa şuna vurdu: " + hit.collider.name);

        if (hit.collider is TerrainCollider tCol)
        {
            tCol.GetComponent<TerrainLayerManager>().PaintSoilServerRpc(hit.point, 1);
            Debug.Log("Terrain boyama komutu gönderildi!");
        }
    }
}