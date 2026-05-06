using UnityEngine;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        // 1. ÖNCE ENERJİ KONTROLÜ YAP
        PlayerEnergy enerji = inv.GetComponent<PlayerEnergy>();
        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Bu eylemi yapmak için yeterli enerjin yok.");
            return; // Gücü yoksa alttaki kodlar hiç çalışmaz, toprağa vuramaz.
        }

        // 2. GÜCÜ YETİYORSA İŞLEMİ YAP
        Debug.Log("Çapa şuna vurdu: " + hit.collider.name);

        if (hit.collider is TerrainCollider tCol)
        {
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