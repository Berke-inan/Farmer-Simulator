using UnityEngine;
using Unity.Netcode;

public class PullukMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;
    public float islemAraligi = 0.1f;
    private float islemSayaci = 0f;

    [Header("Toprak Boyama Noktalarý")]
    [Tooltip("Aþaðýya doðru lazerin atýlacaðý noktalar. (Örneðin: 5 adet demir pivotunu buraya sürükleyin)")]
    public Transform[] lazerNoktalari;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        // Makine çalýþmýyorsa dur
        if (anaGovde == null || !anaGovde.isWorking.Value) return;

        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        if (other is TerrainCollider tCol)
        {
            TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();
            if (manager == null) return;

            bool islemYapildi = false;

            // LÝSTEDEKÝ HER BÝR DEMÝRDEN (NOKTADAN) AYRI AYRI LAZER AT
            foreach (Transform nokta in lazerNoktalari)
            {
                if (nokta == null) continue;

                // Lazerin baþlangýç noktasýný, o anki demirin biraz üstü olarak belirliyoruz
                Vector3 baslangicNoktasi = nokta.position + Vector3.up * 0.5f;

                if (Physics.Raycast(baslangicNoktasi, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider == tCol)
                    {
                        manager.PaintSoilServerRpc(hit.point, 1);
                        islemYapildi = true; // Lazerlerden en az biri topraðý vurdu
                    }
                }
            }

            // Eðer en az bir demir topraðý boyadýysa sayacý sýfýrla ki taramaya devam etsin
            if (islemYapildi)
            {
                islemSayaci = 0f;
            }
        }
    }
}