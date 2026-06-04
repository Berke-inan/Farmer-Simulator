using UnityEngine;
using Unity.Netcode;

public class VehicleStatus : NetworkBehaviour
{
    [Header("Geliþmiþ Durum Ayarlarý")]
    [Tooltip("Traktörün ana gövde veya motor canýný tutan DurabilityManager bileþeni. Elle atandýðýnda kesin sonuç verir.")]
    public DurabilityManager anaGovdeDurumu;

    private DurabilityManager[] tumParcalar;

    public override void OnNetworkSpawn()
    {
        YenileParcalari();
    }

    private void YenileParcalari()
    {
        // Gizli veya inaktif alt nesneleri de bulabilmesi için true parametresi eklendi
        tumParcalar = GetComponentsInChildren<DurabilityManager>(true);
    }

    public bool SurusIcinUygunMu()
    {
        if (tumParcalar == null || tumParcalar.Length == 0) YenileParcalari();
        if (tumParcalar == null || tumParcalar.Length == 0) return true;

        foreach (var parca in tumParcalar)
        {
            if (parca != null && parca.currentHealth.Value <= 0)
            {
                return false;
            }
        }

        return true;
    }

    // --- KESÝN ÇÖZÜM MÜLKÜ (PROPERTY) ---
    public float TraktorDurumYuzdesi
    {
        get
        {
            // 1. ÖNCELÝK: Eðer Inspector'dan ana parça elle seçildiyse direkt onun canýný döndür
            if (anaGovdeDurumu != null && anaGovdeDurumu.maxHealth > 0f)
            {
                return (anaGovdeDurumu.currentHealth.Value / anaGovdeDurumu.maxHealth) * 100f;
            }

            // 2. ÖNCELÝK: Seçilmediyse otomatik filtreleme algoritmasýný çalýþtýr
            if (tumParcalar == null || tumParcalar.Length == 0) YenileParcalari();
            if (tumParcalar == null || tumParcalar.Length == 0) return 100f;

            float toplamYuzde = 0f;
            int gecerliParcaSayisi = 0;

            foreach (var parca in tumParcalar)
            {
                if (parca == null) continue;
                string parcaAdi = parca.gameObject.name.ToLower();

                // Tekerlek elemanlarýný yüzdesel barýn içinden tamamen eliyoruz
                if (parcaAdi.Contains("lastik") || parcaAdi.Contains("wheel") || parcaAdi.Contains("wc"))
                    continue;

                if (parca.maxHealth > 0f)
                {
                    toplamYuzde += (parca.currentHealth.Value / parca.maxHealth) * 100f;
                    gecerliParcaSayisi++;
                }
            }

            return gecerliParcaSayisi > 0 ? (toplamYuzde / gecerliParcaSayisi) : 100f;
        }
    }
}