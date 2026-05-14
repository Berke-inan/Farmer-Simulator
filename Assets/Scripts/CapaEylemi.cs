using UnityEngine;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    // Parametre tipini InventoryManager'dan PlayerInventory'e güncelledik.
    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // 1. ÖNCE ENERJİ KONTROLÜ YAP
        // PlayerInventory bir NetworkBehaviour olduğu için GetComponent ile aynı obje üzerindeki enerjiye ulaşabiliriz.
        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();

        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Bu eylemi yapmak için yeterli enerjin yok.");
            return;
        }

        // 2. GÜCÜ YETİYORSA İŞLEMİ YAP
        if (hit.collider != null)
        {
            Debug.Log("Çapa şuna vurdu: " + hit.collider.name);

            if (hit.collider is TerrainCollider tCol)
            {
                // TerrainLayerManager scriptinin bu metodu desteklediğinden emin ol.
                tCol.GetComponent<TerrainLayerManager>().PaintSoilServerRpc(hit.point, 1, fircaBoyutu);
                Debug.Log("Terrain boyama komutu gönderildi!");

                // 3. İŞLEM BAŞARILI OLDUĞU İÇİN ENERJİYİ DÜŞÜR
                if (enerji != null)
                {
                    enerji.EnerjiHarcaServerRpc(2f);
                }
            }
        }
    }
}