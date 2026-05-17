using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EsyaDususSesi : MonoBehaviour
{
    [Header("Ses Ayarlarý")]
    [Tooltip("Yere düþünce çýkacak sesler (Birden fazla koyarsan rastgele çalar, hep ayný ses çýkmaz ve doðal olur)")]
    public AudioClip[] dusmeSesleri;

    [Tooltip("Sesin en fazla ne kadar yüksek çýkabileceði")]
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Header("Fizik Ayarlarý")]
    [Tooltip("Sesin çýkmasý için eþyanýn ne kadar hýzlý çarpmasý lazým? (Yerde hafif sürüklenirken ses çýkmasýný önler)")]
    public float minimumCarpmaHizi = 1.5f;

    [Tooltip("Ne kadar þiddetli çarparsa ses o kadar yüksek çýkar. Bu deðer maksimum hýzý (en yüksek sesi) belirler.")]
    public float maksimumCarpmaHizi = 10f;

    [Tooltip("Ayný sesin üst üste binmesini engellemek için bekleme süresi (Saniye)")]
    public float sesBeklemeSuresi = 0.15f;

    private AudioSource audioSource;
    private float sonSesCalmaZamani;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Sesi 3D yapar (Uzaklaþtýkça sesi azalýr)
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
        audioSource.playOnAwake = false;
    }

    // Obje baþka bir collider'a (zemine veya duvara) fiziksel olarak çarptýðýnda otomatik çalýþýr
    private void OnCollisionEnter(Collision collision)
    {
        // 1. GÜVENLÝK: Eðer ses atanmamýþsa veya dizi boþsa iptal et
        if (dusmeSesleri == null || dusmeSesleri.Length == 0) return;

        // 2. SPAM KONTROLÜ: Son ses çalýnmasýnýn üzerinden yeterli zaman geçmediyse iptal et
        if (Time.time - sonSesCalmaZamani < sesBeklemeSuresi) return;

        // 3. ÇARPMA ÞÝDDETÝ: Eþyanýn yere (veya baþka bir þeye) çarpma hýzýný ölç
        float carpmaSiddeti = collision.relativeVelocity.magnitude;

        // 4. MANTIK KONTROLÜ: Eðer çok yavaþ dokunduysa (sadece yerde kayýyorsa) ses çýkarma
        if (carpmaSiddeti > minimumCarpmaHizi)
        {
            // Çarpma þiddetine göre ses seviyesini (Volume) ayarla. 
            // Hafif düþerse kýsýk, sert atýlýrsa çok yüksek ses çýkarýr!
            float hesaplananVolume = Mathf.Lerp(0.1f, maxVolume, carpmaSiddeti / maksimumCarpmaHizi);

            // Sesin tonunu (Pitch) çok hafif deðiþtir (Robotik hissi kýrar, doðal duyulur)
            audioSource.pitch = Random.Range(0.85f, 1.15f);

            // Diziden rastgele bir ses seç
            AudioClip secilenSes = dusmeSesleri[Random.Range(0, dusmeSesleri.Length)];

            // Sesi çal ve son çalma zamanýný kaydet
            audioSource.PlayOneShot(secilenSes, hesaplananVolume);
            sonSesCalmaZamani = Time.time;
        }
    }
}