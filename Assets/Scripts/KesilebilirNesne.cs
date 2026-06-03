using UnityEngine;
using Unity.Netcode;

public class KesilebilirNesne : NetworkBehaviour
{
    [Header("Kesilme Ayarlarý")]
    [Tooltip("Normal dekor aðaçlarý veya objeler için gereken varsayýlan vuruþ sayýsý")]
    public int dekorAgacVurusSayisi = 3;

    [Header("Düþen Eþya (Loot) Ayarlarý")]
    [Tooltip("Dekor aðaçlarý kesilince düþecek NORMAL kütük prefabý")]
    public GameObject dekorKutukPrefab;

    [Tooltip("Meyve aðaçlarý kesilince düþecek ÖZEL kütük prefabý")]
    public GameObject meyveKutukPrefab;

    [Tooltip("Büyük/Dekor aðaçlar kesildiðinde kaç adet kütük düþsün?")]
    public int dusenKutukSayisi = 3;

    private int alinanVurus = 0;
    private TreeController agacKontrolcusu;

    private void Awake()
    {
        // Objede TreeController (Meyve aðacý sistemi) var mý diye kontrol ediyoruz
        agacKontrolcusu = GetComponent<TreeController>();
    }

    public void VurusAl()
    {
        VurusAlServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void VurusAlServerRpc()
    {
        int limitVurusSayisi = dekorAgacVurusSayisi;
        int dusurulecekKutukSayisi = dusenKutukSayisi;
        GameObject secilenPrefab = dekorKutukPrefab; // Varsayýlan olarak dekor prefabýný seç

        // --- AKILLI MEYVE AÐACI KONTROLÜ ---
        if (agacKontrolcusu != null)
        {
            // Aðaçta TreeController varsa, düþecek prefabý meyve kütüðü olarak deðiþtir!
            secilenPrefab = meyveKutukPrefab;

            // Eðer aðaç henüz Fide (fidan) aþamasýndaysa: 1 Vuruþta kesilir, ODUN DÜÞMEZ
            if (agacKontrolcusu.mevcutDurum.Value == TreeState.Fide)
            {
                limitVurusSayisi = 1;
                dusurulecekKutukSayisi = 0; // Fideden loot çýkmasýný engelliyoruz
            }
            // Büyümüþ, Meyveli veya Kuru ise normal vuruþ ve loot geçerli
            else
            {
                limitVurusSayisi = 3;
                dusurulecekKutukSayisi = dusenKutukSayisi;
            }
        }

        alinanVurus++;

        if (alinanVurus >= limitVurusSayisi)
        {
            // 1. KÜTÜKLERÝ SPAWN ET (Eðer düþecek kütük sayýsý 0'dan büyükse)
            if (dusurulecekKutukSayisi > 0)
            {
                KutukleriDusur(secilenPrefab, dusurulecekKutukSayisi);
            }

            // 2. AÐACI YOK ET (Despawn)
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    private void KutukleriDusur(GameObject spawnlanacakPrefab, int adet)
    {
        if (spawnlanacakPrefab == null)
        {
            Debug.LogWarning("Kütük Prefab'ý atanmamýþ! Aðaç kesildi ama odun düþmedi.");
            return;
        }

        Vector3 agacPozisyonu = transform.position;

        for (int i = 0; i < adet; i++)
        {
            // Kütükleri Y ekseninde üst üste diz ve hafif rastgelelik kat
            Vector3 kutukPozisyonu = agacPozisyonu + new Vector3(0, (i * 0.7f) + 0.5f, 0);
            kutukPozisyonu += new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f));

            Quaternion rastgeleRotasyon = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));

            GameObject yeniKutuk = Instantiate(spawnlanacakPrefab, kutukPozisyonu, rastgeleRotasyon);

            NetworkObject kutukNetObj = yeniKutuk.GetComponent<NetworkObject>();
            if (kutukNetObj != null)
            {
                kutukNetObj.Spawn();
            }
        }
    }
}