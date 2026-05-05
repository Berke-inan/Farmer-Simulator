using UnityEngine;

public class CapaEylemi : MonoBehaviour, IUseableTool
{

    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;
    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        // Konsola neye vurduğumuzu yazdıralım ki bilelim
        Debug.Log("Çapa şuna vurdu: " + hit.collider.name);

        if (hit.collider is TerrainCollider tCol)
        {
            tCol.GetComponent<TerrainLayerManager>().PaintSoilServerRpc(hit.point, 1,fircaBoyutu);
            Debug.Log("Terrain boyama komutu gönderildi!");


            // --- ENERJİ DÜŞÜRME KISMI ---
            // İşlemi yapan oyuncunun (inv) üzerindeki Enerji sistemini bul
            PlayerEnergy enerji = inv.GetComponent<PlayerEnergy>();
            if (enerji != null)
            {
                // Çapa vurulduğu için 2 enerji harca 
                enerji.EnerjiHarcaServerRpc(2f);
            }
        }

    }
}