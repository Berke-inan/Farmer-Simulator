using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class SepetEsyasi : MonoBehaviour, IUseableTool
{
    // YENÝ YAPI: ID'ye göre görsel setlerini tutan yardýmcý sýnýf
    [System.Serializable]
    public class GorselSeti
    {
        public int itemID; // Örn: 15 (Yumurta)
        public GameObject[] gorselModeller; // O ID'ye ait 10 adet görsel obje
    }

    [Header("Görsel Ayarlarý")]
    [Tooltip("ID bazlý görsel setlerini buraya tanýmlayýn (Örn: ID 15 için Yumurta objeleri)")]
    public List<GorselSeti> gorselSetleri = new List<GorselSeti>();

    [Header("Sepet Ayarlarý")]
    public int maxKapasite = 10;

    [Header("Toplanabilir Eþya Filtresi")]
    public List<int> kabulEdilenItemIDler = new List<int>();

    private PlayerInventory inventory;
    private Dictionary<int, GameObject[]> gorselVeritabaný = new Dictionary<int, GameObject[]>();

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();

        // Hýzlý eriþim için listeyi Dictionary'ye çeviriyoruz
        foreach (var set in gorselSetleri)
        {
            if (!gorselVeritabaný.ContainsKey(set.itemID))
            {
                gorselVeritabaný.Add(set.itemID, set.gorselModeller);
            }
        }

        if (inventory != null)
        {
            // Ýlk açýlýþta mevcut duruma göre güncelle
            GorselleriGuncelle();

            // YENÝ: sepetDoluluk yerine sepetIcerikIDleri listesindeki deðiþimi dinliyoruz
            inventory.sepetIcerikIDleri.OnListChanged += DolulukDegistigindeGorselleriGuncelle;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.sepetIcerikIDleri.OnListChanged -= DolulukDegistigindeGorselleriGuncelle;
        }
    }

    // NetworkList deðiþim olayý (event) parametreleri
    private void DolulukDegistigindeGorselleriGuncelle(NetworkListEvent<int> changeEvent)
    {
        GorselleriGuncelle();
    }

    // ANA GÖRSEL GÜNCELLEME MANTIÐI
    private void GorselleriGuncelle()
    {
        // Önce tüm ID setlerinin tüm görsellerini kapatarak temizlik yap
        foreach (var set in gorselVeritabaný.Values)
        {
            foreach (var obje in set)
            {
                if (obje != null) obje.SetActive(false);
            }
        }

        // Sepette eþya yoksa iþlem bitti (hepsi kapalý kaldý)
        if (inventory == null || inventory.sepetIcerikIDleri.Count == 0) return;

        // Sepet Doluluðu
        int durum = inventory.sepetIcerikIDleri.Count;

        // KRÝTÝK KISIM: Sepetin "Türü" ne?
        // Karýþýk sepet olmamasý için listenin ÝLK elemanýnýn ID'sine bakýyoruz.
        int sepetTuruID = inventory.sepetIcerikIDleri[0];

        // Bu ID'ye ait görseller veritabanýmýzda var mý?
        if (gorselVeritabaný.TryGetValue(sepetTuruID, out GameObject[] aktifGorseller))
        {
            // Sadece bu türe ait görselleri sýrayla aç
            for (int i = 0; i < aktifGorseller.Length; i++)
            {
                if (aktifGorseller[i] != null)
                {
                    aktifGorseller[i].SetActive(i < durum);
                }
            }
        }
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        // 1. Aðaçtan meyve toplama
        TreeController agac = hit.collider.GetComponentInParent<TreeController>();
        if (agac != null && agac.mevcutDurum.Value == TreeState.Meyveli)
        {
            if (inv.sepetDoluluk.Value >= maxKapasite) return;
            inv.SepeteMeyveToplaServerRpc(agac.NetworkObjectId);
            return;
        }

        // 2. Yerden eþya toplama
        InteractableItem yerdekiEsya = hit.collider.GetComponentInParent<InteractableItem>();
        if (yerdekiEsya != null)
        {
            if (!kabulEdilenItemIDler.Contains(yerdekiEsya.itemID)) return;
            if (inv.sepetDoluluk.Value >= maxKapasite) return;
            inv.SepeteYerdenEsyaToplaServerRpc(yerdekiEsya.GetComponent<NetworkObject>().NetworkObjectId);
            return;
        }

        // 3. BOÞALTMA MANTIÐI
        if (inv.sepetDoluluk.Value > 0)
        {
            Vector3 bosaltmaNoktasi = hit.point + (Vector3.up * 0.5f);
            inv.SepettenEsyaBosaltServerRpc(bosaltmaNoktasi);
        }
    }
}