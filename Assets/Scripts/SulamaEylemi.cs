using UnityEngine;

public class SulamaEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            // Sadece çapalanmış yerler sulanabilir
            if (manager.IsSoilTilled(hit.point))
            {
                // Toprağı ıslak dokuya boya, gerisini bitkiler halledecek
                manager.PaintSoilServerRpc(hit.point, manager.wetLayerIndex);
            }
        }
    }
}