using UnityEngine;
using Unity.Netcode;

public class MakineTetikleyici : NetworkBehaviour
{
    [Header("Makine Durumu")]
    // TEST ÝÇÝN GEÇÝCÝ OLARAK TRUE YAPTIK (Makine hep açýk baþlasýn)
    public NetworkVariable<bool> isWorking = new NetworkVariable<bool>(true);

    private IUseableTool uzerindekiAlet;

    private void Awake()
    {
        uzerindekiAlet = GetComponent<IUseableTool>();

        // Eðer CapaEylemi kodunu bulamazsa bizi uyaracak
        if (uzerindekiAlet == null)
        {
            Debug.LogError("DÝKKAT: Pulluðun üzerinde IUseableTool (CapaEylemi vb.) kodu bulunamadý!");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 1. Sensör bir þeye deðdiðinde Konsola yaz:
        Debug.Log("Sensör bir þeye deðdi: " + other.gameObject.name);

        if (!IsServer)
        {
            Debug.LogWarning("Ýþlem Ýptal: Siz Server (Host) deðilsiniz!");
            return;
        }

        if (!isWorking.Value)
        {
            Debug.Log("Ýþlem Ýptal: Makine kapalý (isWorking = false)");
            return;
        }

        if (uzerindekiAlet == null) return;

        // 2. Deðdiði þey Toprak mý?
        if (other.TryGetComponent(out SoilTile toprak))
        {
            Debug.Log("TOPRAK BULUNDU! Topraðýn þu anki durumu: " + toprak.MevcutDurum);

            // 3. Alete eylem yapmasýný söyle
            uzerindekiAlet.EylemYap(toprak, null);
        }
    }
}