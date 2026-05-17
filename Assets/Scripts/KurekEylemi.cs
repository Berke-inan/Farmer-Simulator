using UnityEngine;
using System.Collections;

public class KurekEylemi : MonoBehaviour, IUseableTool
{
    [Header("Kürek Ayarlarý")]
    public int fircaBoyutu = 3;
    public float harcananEnerji = 2f;
    public float maxVurusMesafesi = 2.5f;

    [Header("Görsel ve Ses Ayarlarý (Animasyon)")]
    [Tooltip("Tüm bu animasyonun toplam süresi (Saniye)")]
    public float toplamAnimasyonSuresi = 0.7f;

    [Tooltip("Küreðin hareket geniþliði. Ekranda çok ileri/geri gidiyorsa bu sayýyý küçült (Örn: 0.1)")]
    public float hareketSiddeti = 0.2f;

    [Tooltip("Vuruþ anýndaki açý (Aþaðý eðilme)")]
    public Vector3 vurusAcisi = new Vector3(65f, 0f, 0f);
    [Tooltip("Topraðý atma anýndaki açý (Yana dönme/Scoop)")]
    public Vector3 atmaAcisi = new Vector3(20f, 0f, 45f);

    [Header("Efektler")]
    public AudioClip vurusSesi;
    public GameObject tozEfektiPrefab;

    private AudioSource audioSource;
    private Quaternion orijinalRotasyon;
    private Vector3 orijinalPozisyon; // YENÝ: Artýk pozisyonu da hafýzaya alýyoruz
    private bool isSwinging = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (isSwinging) return;

        // Pozisyon ve rotasyonu tam týklama anýnda alýyoruz
        orijinalRotasyon = transform.localRotation;
        orijinalPozisyon = transform.localPosition;

        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();
        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Kürek kullanmak için yeterli enerjin yok.");
            return;
        }

        // MESAFE KONTROLÜ
        bool bosaSalladi = true;
        TerrainCollider tCol = null;

        if (hit.collider != null)
        {
            float mesafe = Vector3.Distance(inventory.transform.position, hit.point);
            if (mesafe <= maxVurusMesafesi && hit.collider is TerrainCollider)
            {
                bosaSalladi = false;
                tCol = hit.collider as TerrainCollider;
            }
        }

        StartCoroutine(KurekVurAnimasyonu(hit, tCol, enerji, bosaSalladi));
    }

    private IEnumerator KurekVurAnimasyonu(RaycastHit hit, TerrainCollider tCol, PlayerEnergy enerji, bool bosaSalladi)
    {
        isSwinging = true;

        // =================================================================
        // HEDEF POZÝSYONLAR VE AÇILAR (Koreografi Duraklarý)
        // =================================================================

        // 1. Geri Çekilme (Wind-up): Hafifçe geri, yukarý ve arkaya yatýk
        Vector3 geriPos = orijinalPozisyon + new Vector3(0f, 0.5f, -1f) * hareketSiddeti;
        Quaternion geriRot = orijinalRotasyon * Quaternion.Euler(-20f, 0f, 0f);

        // 2. Vuruþ (Impact): Ýleri, aþaðý ve öne yatýk
        Vector3 vurusPos = orijinalPozisyon + new Vector3(0f, -1f, 1.5f) * hareketSiddeti;
        Quaternion vurusRot = orijinalRotasyon * Quaternion.Euler(vurusAcisi);

        // 3. Fýrlatma (Throw): Yukarý, saða ve yana yatýk (Scoop hareketi)
        Vector3 atmaPos = orijinalPozisyon + new Vector3(1f, 0.8f, 0.5f) * hareketSiddeti;
        Quaternion atmaRot = orijinalRotasyon * Quaternion.Euler(atmaAcisi);

        // =================================================================
        // AÞAMA 1: GERÝ ÇEKÝLME (Toplam sürenin %15'i)
        // =================================================================
        yield return StartCoroutine(HareketEt(orijinalPozisyon, geriPos, orijinalRotasyon, geriRot, toplamAnimasyonSuresi * 0.15f));

        // =================================================================
        // AÞAMA 2: VURUÞ (Toplam sürenin %20'si)
        // =================================================================
        yield return StartCoroutine(HareketEt(geriPos, vurusPos, geriRot, vurusRot, toplamAnimasyonSuresi * 0.20f));

        // ---> TAM BU ANDA TOPRAÐA VURDUK! EFEKTLER VE ÝÞLEMLER BURADA ÇALIÞIR <---
        if (!bosaSalladi)
        {
            if (vurusSesi != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(vurusSesi);
            }

            if (tozEfektiPrefab != null)
            {
                Vector3 dogumNoktasi = hit.point + (Vector3.up * 0.15f);
                GameObject toz = Instantiate(tozEfektiPrefab, dogumNoktasi, Quaternion.LookRotation(hit.normal));
                Destroy(toz, 2f);
            }

            var manager = tCol.GetComponent<TerrainLayerManager>();
            if (manager != null)
            {
                manager.PaintSoilServerRpc(hit.point, 0, fircaBoyutu);
                if (enerji != null) enerji.EnerjiHarcaServerRpc(harcananEnerji);
            }
        }

        // =================================================================
        // AÞAMA 3: KAZIP FIRLATMA (Toplam sürenin %40'ý)
        // =================================================================
        yield return StartCoroutine(HareketEt(vurusPos, atmaPos, vurusRot, atmaRot, toplamAnimasyonSuresi * 0.40f));

        // =================================================================
        // AÞAMA 4: TOPARLANMA / ESKÝ HALÝNE DÖNÜÞ (Toplam sürenin %25'i)
        // =================================================================
        yield return StartCoroutine(HareketEt(atmaPos, orijinalPozisyon, atmaRot, orijinalRotasyon, toplamAnimasyonSuresi * 0.25f));

        // Güvenlik: Ýþlem bitince her þeyi kusursuzca orijinal yerine oturt
        transform.localPosition = orijinalPozisyon;
        transform.localRotation = orijinalRotasyon;
        isSwinging = false;
    }

    // Pozisyon ve rotasyonu ayný anda yumuþakça deðiþtiren yardýmcý fonksiyon
    private IEnumerator HareketEt(Vector3 baslangicPos, Vector3 hedefPos, Quaternion baslangicRot, Quaternion hedefRot, float sure)
    {
        float gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;

            // Yumuþak geçiþ (Ease-in / Ease-out) için SmoothStep kullanýyoruz
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition = Vector3.Lerp(baslangicPos, hedefPos, smoothT);
            transform.localRotation = Quaternion.Slerp(baslangicRot, hedefRot, smoothT);

            yield return null;
        }
    }
}