using UnityEngine;
using System.Collections;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    [Header("Çapa Ayarları")]
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 3;
    [Tooltip("Her vuruşta harcanacak enerji miktarı")]
    public float harcananEnerji = 2f;

    [Tooltip("Çapanın toprağa etki edebilmesi için maksimum mesafe (Metre)")]
    public float maxVurusMesafesi = 2.5f; // UZAKLIK SINIRI BURADA

    [Header("Görsel ve Ses Ayarları (Animasyon)")]
    [Tooltip("Çapanın vuruş açısı. Çapa yanlış yöne dönüyorsa X, Y, Z değerlerini deneyerek bulabilirsin.")]
    public Vector3 vurusAcisi = new Vector3(65f, 0f, 0f); // İLK SİSTEMDEKİ VECTOR3 MANTIĞI GERİ GELDİ

    [Tooltip("Vuruşun aşağı inip geri gelme süresi")]
    public float vurusSuresi = 0.4f;

    [Tooltip("Toprağa vurma sesi")]
    public AudioClip vurusSesi;
    [Tooltip("Vurduğunda çıkacak toz efekti (Prefab)")]
    public GameObject tozEfektiPrefab;

    private AudioSource audioSource;
    private Quaternion orijinalRotasyon;
    private bool isSwinging = false; // Spam tıklamayı önler

    private void Awake()
    {
        // Objenin üzerinde AudioSource yoksa kodla ekle
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // Sesi 3D yapar
        }
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // 1. SPAM KONTROLÜ: Eğer çapa zaten havadaysa yeni vuruşu engelle
        if (isSwinging) return;

        // DÜZELTME: Orijinal rotasyonu Awake yerine tam eylemin başladığı karede kaydediyoruz.
        orijinalRotasyon = transform.localRotation;

        // 2. ENERJİ KONTROLÜ
        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();

        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Bu eylemi yapmak için yeterli enerjin yok.");
            return;
        }

        // 3. MESAFE KONTROLÜ (UZAKTAKİLERİ ENGELLEME)
        bool bosaSalladi = true;
        TerrainCollider tCol = null;

        if (hit.collider != null)
        {
            // Oyuncunun konumu ile tıklanan yer arasındaki mesafeyi ölçüyoruz
            float mesafe = Vector3.Distance(inventory.transform.position, hit.point);

            if (mesafe <= maxVurusMesafesi)
            {
                // Yakınsak ve vurulan şey topraksa işlemi onayla
                if (hit.collider is TerrainCollider)
                {
                    bosaSalladi = false;
                    tCol = hit.collider as TerrainCollider;
                }
            }
            else
            {
                Debug.Log($"Çok uzaksın! Mesafe: {mesafe:F1}m");
            }
        }

        // 4. VURUŞ ANİMASYONUNU BAŞLAT
        StartCoroutine(CapaVurAnimasyonu(hit, tCol, enerji, bosaSalladi));
    }

    private IEnumerator CapaVurAnimasyonu(RaycastHit hit, TerrainCollider tCol, PlayerEnergy enerji, bool bosaSalladi)
    {
        isSwinging = true;

        // İLK SİSTEMDEKİ HEDEF ROTASYON HESAPLAMASI
        Quaternion hedefRotasyon = orijinalRotasyon * Quaternion.Euler(vurusAcisi);

        float gecenZaman = 0f;
        float yariSure = vurusSuresi / 2f;

        // 1. AŞAMA: ÇAPAYI AŞAĞI İNDİR
        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;

            // İlk sistemdeki yumuşak Slerp hareketi
            transform.localRotation = Quaternion.Slerp(orijinalRotasyon, hedefRotasyon, t * t);
            yield return null;
        }

        // İnme işlemini tam açıya sabitle
        transform.localRotation = hedefRotasyon;

        // ==========================================
        // 2. AŞAMA: TAM VURUŞ (IMPACT) ANI!
        // ==========================================
        if (!bosaSalladi)
        {
            // A) Sesi Çal
            if (vurusSesi != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(vurusSesi);
            }

            // B) Toz Efektini Yarat
            if (tozEfektiPrefab != null)
            {
                Vector3 dogumNoktasi = hit.point + (Vector3.up * 0.15f);
                GameObject toz = Instantiate(tozEfektiPrefab, dogumNoktasi, Quaternion.LookRotation(hit.normal));
                Destroy(toz, 2f);
            }

            // C) Toprağı Boya ve Enerjiyi Kes (SENİN KENDİ KODUN)
            var manager = tCol.GetComponent<TerrainLayerManager>();
            if (manager != null)
            {
                manager.PaintSoilServerRpc(hit.point, 1, fircaBoyutu);

                if (enerji != null)
                {
                    enerji.EnerjiHarcaServerRpc(harcananEnerji);
                }
            }
        }

        // ==========================================
        // 3. AŞAMA: ÇAPAYI YUKARI KALDIR (Geri Çekme)
        // ==========================================
        gecenZaman = 0f;
        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;

            transform.localRotation = Quaternion.Slerp(hedefRotasyon, orijinalRotasyon, t);
            yield return null;
        }

        // Son rotasyonu orijinal haline sabitle
        transform.localRotation = orijinalRotasyon;
        isSwinging = false; // Bir sonraki vuruşa hazırız!
    }
}