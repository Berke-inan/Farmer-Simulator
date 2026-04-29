using UnityEngine;
using Unity.Netcode;

public class MakineTetikleyici : NetworkBehaviour
{
    private IUseableTool uzerindekiAlet;
    private AttachableEquipment anaGovde; // Ekipmanýn ana beyni

    private void Awake()
    {
        uzerindekiAlet = GetComponent<IUseableTool>();
        anaGovde = GetComponent<AttachableEquipment>(); // Kendi objesindeki AttachableEquipment'ý bulur

        if (uzerindekiAlet == null)
        {
            Debug.LogError("DÝKKAT: Pulluðun üzerinde IUseableTool (CapaEylemi vb.) kodu bulunamadý!");
        }

        if (anaGovde == null)
        {
            Debug.LogError("DÝKKAT: Makinenin üzerinde AttachableEquipment kodu bulunamadý!");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Debug.Log("Sensör bir þeye deðdi: " + other.gameObject.name);

        if (!IsServer) return;

        // --- DEÐÝÞEN KISIM: Ýzni anaGövde'den (AttachableEquipment) alýyoruz ---
        if (anaGovde == null || !anaGovde.isWorking.Value)
        {
            // Ýstersen konsol kirlenmesin diye buradaki Debug.Log'u silebilirsin
            // Debug.Log("Ýþlem Ýptal: Makine kapalý"); 
            return;
        }

        if (uzerindekiAlet == null) return;

        // Deðdiði þey Toprak mý?
        if (other.TryGetComponent(out SoilTile toprak))
        {
            // Debug.Log("TOPRAK BULUNDU! Topraðýn þu anki durumu: " + toprak.MevcutDurum);

            // Alete eylem yapmasýný söyle
            uzerindekiAlet.EylemYap(toprak, null);
        }
    }
}