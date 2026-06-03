using UnityEngine;
using Unity.Netcode;

public class PullukMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;
    private BreakDisableBehavior bozulmaKontrolu;
    public float islemAraligi = 0.1f;
    private float islemSayaci = 0f;

    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
        bozulmaKontrolu = GetComponent<BreakDisableBehavior>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (bozulmaKontrolu != null && bozulmaKontrolu.isBroken.Value) return;
        if (!IsServer) return;

        if (anaGovde == null || !anaGovde.isWorking.Value)
        {
            return;
        }

        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        if (other is TerrainCollider tCol)
        {
            Debug.Log("ADIM 2: Terrain (Toprak) algýlandý! Lazer atýlýyor...");

            Vector3 baslangicNoktasi = transform.position + Vector3.up * 0.5f;

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
                        Debug.LogError("HATA: Terrain üzerinde 'TerrainLayerManager' kodu bulunamadý!");
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