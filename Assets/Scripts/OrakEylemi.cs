using UnityEngine;
using System.Collections;

public class OrakEylemi : MonoBehaviour, IUseableTool
{
    [Header("Orak Ayarları")]
    [Tooltip("Her başarılı hasatta harcanacak enerji miktarı")]
    public float harcananEnerji = 1.5f;

    [Tooltip("Orağın ekine ulaşabilmesi için maksimum mesafe (Metre)")]
    public float maxVurusMesafesi = 2.5f;

    [Header("Görsel ve Ses Ayarları (Animasyon)")]
    [Tooltip("Tüm animasyonun toplam süresi")]
    public float toplamAnimasyonSuresi = 0.5f;

    [Tooltip("Vuruş öncesi gerilme (Sağa çekilme) açısı. Y ekseni ile oynayarak yönü bulabilirsin.")]
    public Vector3 hazirlikAcisi = new Vector3(0f, 30f, 0f);
    [Tooltip("Vuruş anındaki savurma (Sola biçme) açısı.")]
    public Vector3 vurusAcisi = new Vector3(0f, -75f, 0f);

    [Tooltip("Orağın sağa/sola ne kadar geniş savrulacağı (0.1 ila 0.3 arası iyidir)")]
    public float hareketSiddeti = 0.2f;

    [Header("Efektler")]
    [Tooltip("Ekin biçildiğinde çıkacak hasat/kesme sesi")]
    public AudioClip kesmeSesi;

    private AudioSource audioSource;
    private Quaternion orijinalRotasyon;
    private Vector3 orijinalPozisyon;
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

        // Pozisyon ve rotasyonu tam tıklama anında alıyoruz
        orijinalRotasyon = transform.localRotation;
        orijinalPozisyon = transform.localPosition;

        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();
        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Orak kullanmak için yeterli enerjin yok.");
            return;
        }

        // MESAFE VE HEDEF KONTROLÜ
        ModularCrop hedefEkin = null;
        bool mesafedeMi = false;

        if (hit.collider != null)
        {
            float mesafe = Vector3.Distance(inventory.transform.position, hit.point);
            if (mesafe <= maxVurusMesafesi)
            {
                mesafedeMi = true;

                // Vurduğumuz şey ekin mi diye kontrol et
                hit.collider.TryGetComponent(out hedefEkin);
            }
            else
            {
                Debug.Log($"Çok uzaksın! Mesafe: {mesafe:F1}m");
            }
        }

        // Animasyonu başlat
        StartCoroutine(OrakVurAnimasyonu(hedefEkin, inventory, enerji, mesafedeMi));
    }

    private IEnumerator OrakVurAnimasyonu(ModularCrop hedefEkin, PlayerInventory inventory, PlayerEnergy enerji, bool mesafedeMi)
    {
        isSwinging = true;

        // 1. Geri Çekilme (Wind-up): Hafifçe sağa ve arkaya çekilme
        Vector3 geriPos = orijinalPozisyon + new Vector3(1f, 0f, -0.5f) * hareketSiddeti;
        Quaternion geriRot = orijinalRotasyon * Quaternion.Euler(hazirlikAcisi);

        // 2. Savurma/Biçme (Swing): Hızla sola doğru geniş bir kavis
        Vector3 vurusPos = orijinalPozisyon + new Vector3(-1.2f, 0f, 0.5f) * hareketSiddeti;
        Quaternion vurusRot = orijinalRotasyon * Quaternion.Euler(vurusAcisi);

        // AŞAMA 1: GERİ ÇEKİL (Sürenin %25'i)
        yield return StartCoroutine(HareketEt(orijinalPozisyon, geriPos, orijinalRotasyon, geriRot, toplamAnimasyonSuresi * 0.25f));

        // AŞAMA 2: SOLA BİÇME (Sürenin %25'i - Çok daha hızlı ve sert bir hareket)
        yield return StartCoroutine(HareketEt(geriPos, vurusPos, geriRot, vurusRot, toplamAnimasyonSuresi * 0.25f));

        // ==========================================
        // TAM BİÇME ANI (IMPACT)
        // ==========================================
        if (mesafedeMi && hedefEkin != null)
        {
            // Sesi Çal
            if (kesmeSesi != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.15f);
                audioSource.PlayOneShot(kesmeSesi);
            }

            // SENİN KENDİ EKİN MANTIĞIN:
            if (hedefEkin.IsGrown)
            {
                // Hasat komutunu gönder
                inventory.HasatEtServerRpc(hedefEkin.NetworkObjectId, hedefEkin.transform.position);

                // Enerjiyi sadece başarılı hasatta düşürüyoruz (İstersen her vuruşta da düşürebilirsin)
                if (enerji != null)
                {
                    enerji.EnerjiHarcaServerRpc(harcananEnerji);
                }
            }
            else if (hedefEkin.IsRotted)
            {
                Debug.Log("Bu ekin çürümüş!");
            }
            else
            {
                Debug.Log("Bu ekin henüz büyümedi!");
            }
        }

        // AŞAMA 3: TOPARLANMA VE ESKİ HALİNE DÖNÜŞ (Sürenin %50'si - Yavaşça toparlanır)
        yield return StartCoroutine(HareketEt(vurusPos, orijinalPozisyon, vurusRot, orijinalRotasyon, toplamAnimasyonSuresi * 0.50f));

        transform.localPosition = orijinalPozisyon;
        transform.localRotation = orijinalRotasyon;
        isSwinging = false;
    }

    private IEnumerator HareketEt(Vector3 baslangicPos, Vector3 hedefPos, Quaternion baslangicRot, Quaternion hedefRot, float sure)
    {
        float gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition = Vector3.Lerp(baslangicPos, hedefPos, smoothT);
            transform.localRotation = Quaternion.Slerp(baslangicRot, hedefRot, smoothT);

            yield return null;
        }
    }
}