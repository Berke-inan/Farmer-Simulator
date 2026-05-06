using UnityEngine;
using Unity.Netcode;

public class PullukMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;
    public float islemAraligi = 0.1f;
    private float islemSayaci = 0f;


    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;
    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
    }

    private void OnTriggerStay(Collider other)
    {
        // 1. AÞAMA: Kutu bir þeye deðiyor mu?
        Debug.Log("ADIM 1: Sensör þuna deðiyor -> " + other.gameObject.name);

        if (!IsServer) return;

        // 2. AÞAMA: Makine çalýþýyor mu?
        if (anaGovde == null || !anaGovde.isWorking.Value)
        {
            // Konsol kirlenmesin diye burayý kapalý tutuyoruz, V'ye basýldýðýndan eminiz.
            return;
        }

        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        // 3. AÞAMA: Deðdiði þey Terrain mi?
        if (other is TerrainCollider tCol)
        {
            Debug.Log("ADIM 2: Terrain (Toprak) algýlandý! Lazer atýlýyor...");

            Vector3 baslangicNoktasi = transform.position + Vector3.up * 0.5f;

            // DÝKKAT: QueryTriggerInteraction.Ignore ekledik! 
            // Çünkü lazer yanlýþlýkla senin kendi sensörüne (Box Collider) çarpýp topraðý göremiyor olabilirdi.
            if (Physics.Raycast(baslangicNoktasi, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("ADIM 3: Lazerin çarptýðý tam obje -> " + hit.collider.gameObject.name);

                if (hit.collider == tCol)
                {
                    TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();

                    if (manager != null)
                    {
                        Debug.Log("ADIM 4: HER ÞEY KUSURSUZ! Boyama komutu gönderildi.");
                        manager.PaintSoilServerRpc(hit.point, 1, fircaBoyutu);

                        islemSayaci = 0f;
                    }
                    else
                    {
                        Debug.LogError("HATA: Terrain üzerinde 'TerrainLayerManager' kodu bulunamadý! Arkadaþýn bu kodu nereye koydu?");
                    }
                }
            }
            else
            {
                Debug.LogWarning("HATA: Lazer hiçbir þeye çarpmadý! Sensör çok mu havada?");
            }
        }
    }
}