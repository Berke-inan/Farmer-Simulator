using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class TractorFXController : NetworkBehaviour
{
    private TractorController tractorController;
    private Rigidbody rb;

    [Header("Partikül Sistemleri")]
    [Tooltip("Traktörün egzoz dumaný objesi")]
    public ParticleSystem exhaustSmoke;
    [Tooltip("Arka tekerleklerin çamur fýrlatma objeleri")]
    public ParticleSystem[] wheelMuds;

    [Header("Duman Ayarlarý")]
    public float idleSmokeRate = 15f;    // Rölantide (Dururken) çýkan duman
    public float maxSmokeRate = 70f;     // Tam gaz giderken çýkan duman

    [Header("Çamur Ayarlarý")]
    public float minSpeedForMud = 2.5f;  // Çamurun fýrlamaya baþlayacaðý minimum hýz
    public float maxMudEmission = 80f;   // En yüksek hýzdaki çamur miktarý
    public float maxSpeed = 15f;         // Aracýn tahmini son hýzý

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();

        // Güvenlik Uyarýsý: Inspector'da objeleri sürüklemeyi unutursan konsolda uyarýr
        if (exhaustSmoke == null)
            Debug.LogWarning($"{gameObject.name} üzerinde 'Exhaust Smoke' partikülü eksik!");
    }

    private void Update()
    {
        // 1. KONTROL: Traktörün içinde biri var mý?
        // (Senin kusursuz çalýþan IsOccupied deðiþkenini kullanýyoruz)
        bool isPlayerIn = tractorController.IsOccupied;

        // EÐER TRAKTÖR BOÞSA: Tüm efektleri durdur ve Update'i burada kes.
        if (!isPlayerIn)
        {
            if (exhaustSmoke != null && exhaustSmoke.isPlaying) StopAllEffects();
            return;
        }

        // 2. HIZ OKUMA: Aracýn o anki gerçek fiziksel hýzýný al
        float currentSpeed = rb.linearVelocity.magnitude;

        // 3. EGZOZ DUMANI YÖNETÝMÝ
        if (exhaustSmoke != null)
        {
            // Eðer partikül durmuþsa, tekrar çalýþtýr
            if (!exhaustSmoke.isPlaying) exhaustSmoke.Play();

            // Partikülün "Emission" (Üretim) modülüne eriþ
            var emission = exhaustSmoke.emission;

            // KESÝN ÇÖZÜM: Inspector'da Emission tikini unuttuysan kod zorla açar!
            if (!emission.enabled) emission.enabled = true;

            // Hýza göre duman yoðunluðunu hesapla (Yavaþken idleRate, Hýzlýyken maxRate)
            float speedFactor = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
            float targetRate = Mathf.Lerp(idleSmokeRate, maxSmokeRate, speedFactor);

            // Hesaplanan dumaný sisteme uygula
            emission.rateOverTime = targetRate;

            // HATA AYIKLAMA (Eðer duman çýkmazsa baþýndaki // iþaretini silip konsola bak)
               Debug.Log($"Duman Çalýyor: {exhaustSmoke.isPlaying} | Hedef Duman: {targetRate} | Mevcut: {emission.rateOverTime.constant}");
        }

        // 4. ÇAMUR YÖNETÝMÝ
        ManageMud(currentSpeed);
    }

    private void ManageMud(float speed)
    {
        float mudRate = 0f;

        // Sadece belirli bir hýzý geçerse çamur atmaya baþla
        if (speed >= minSpeedForMud)
        {
            float speedFactor = Mathf.InverseLerp(minSpeedForMud, maxSpeed, speed);
            mudRate = Mathf.Lerp(0, maxMudEmission, speedFactor);
        }

        // Listedeki tüm tekerlek çamurlarýna bu ayarý uygula
        foreach (var mud in wheelMuds)
        {
            if (mud != null)
            {
                var emission = mud.emission;

                if (mudRate > 0 && !mud.isPlaying) mud.Play();

                emission.rateOverTime = mudRate;

                if (mudRate <= 0 && mud.isPlaying) mud.Stop();
            }
        }
    }

    private void StopAllEffects()
    {
        // Traktörden inildiði an tüm partikülleri anýnda kesen güvenlik metodu
        if (exhaustSmoke != null) exhaustSmoke.Stop();

        foreach (var mud in wheelMuds)
        {
            if (mud != null) mud.Stop();
        }
    }
}