using UnityEngine;
using Unity.Netcode;

public class MakineTetikleyici : NetworkBehaviour
{
    private IUseableTool uzerindekiAlet;
    private AttachableEquipment anaGovde;

    [Header("Optimizasyon")]
    [Tooltip("Makine toprağı saniyede kaç kere işlesin? Çok düşük olursa FPS düşer.")]
    public float islemAraligi = 0.1f;
    private float islemSayaci = 0f;

    private void Awake()
    {
        uzerindekiAlet = GetComponent<IUseableTool>();
        anaGovde = GetComponent<AttachableEquipment>();

        if (uzerindekiAlet == null)
        {
            Debug.LogError("DİKKAT: Pulluğun üzerinde IUseableTool (CapaEylemi vb.) kodu bulunamadı!");
        }

        if (anaGovde == null)
        {
            Debug.LogError("DİKKAT: Makinenin üzerinde AttachableEquipment kodu bulunamadı!");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        if (anaGovde == null || !anaGovde.isWorking.Value) return;

        if (uzerindekiAlet == null) return;

        // Performans için bekleme süresi kontrolü
        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        // Değdiği şey Terrain mi?
        if (other is TerrainCollider)
        {
            // Aletler RaycastHit beklediği için, sensörün biraz yukarısından aşağı doğru ışın atıyoruz
            Vector3 baslangicNoktasi = transform.position + Vector3.up * 0.5f;

            if (Physics.Raycast(baslangicNoktasi, Vector3.down, out RaycastHit hit, 2f))
            {
                if (hit.collider is TerrainCollider)
                {
                    // Işın toprağı vurduğunda aleti çalıştırıyoruz
                    // Envanter (PlayerInventory) parametresi makine için null gönderilir
                    uzerindekiAlet.EylemYap(hit, null);

                    // İşlem başarılı olunca sayacı sıfırla
                    islemSayaci = 0f;
                }
            }
        }
    }
}