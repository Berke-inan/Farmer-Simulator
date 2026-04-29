using UnityEngine;
using Unity.Netcode;

public class DayNightCycleManager : NetworkBehaviour
{
    [Header("Zaman Ayarlarý")]
    [Tooltip("Gerçek hayattaki kaç saniye, oyunda 1 tam gün (24 saat) sürsün? Örn: 1200 = 20 dakika")]
    public float gercekSaniyedeBirGun = 1200f;

    // Að üzerinden senkronize edilen saat (0.00 ile 24.00 arasý)
    // Sadece Server deðiþtirebilir, herkes okuyabilir.
    public NetworkVariable<float> guncelSaat = new NetworkVariable<float>(8f); // Sabah 8'de baþlasýn

    [Header("Görsel Ayarlar")]
    [Tooltip("Sahnedeki Directional Light (Güneþ) objesini buraya sürükleyin")]
    public Light gunesIsigi;

    [Tooltip("Güneþin þiddeti saate göre nasýl deðiþsin?")]
    public AnimationCurve gunesSiddeti = new AnimationCurve(
        new Keyframe(0f, 0f),   // Gece yarýsý (Saat 00:00) ýþýk 0
        new Keyframe(5f, 0f),   // Sabaha karþý ýþýk 0
        new Keyframe(7f, 1f),   // Sabah 7'de ýþýk tam güç
        new Keyframe(17f, 1f),  // Akþam 5'te hala tam güç
        new Keyframe(19f, 0f),  // Akþam 7'de (Gün batýmý) ýþýk 0
        new Keyframe(24f, 0f)   // Gece yarýsý ýþýk 0
    );

    private void Update()
    {
        // 1. ZAMANI SADECE SERVER ÝLERLETÝR
        if (IsServer)
        {
            ZamaniIlerlet();
        }

        // 2. GÖRÜNTÜYÜ HERKES (Server + Clientlar) GÜNCELLER
        GorselleriGuncelle();
    }

    private void ZamaniIlerlet()
    {
        // 1 saniyede ne kadar oyun saati geçmeli?
        float saatCarpani = 24f / gercekSaniyedeBirGun;

        guncelSaat.Value += Time.deltaTime * saatCarpani;

        // Gece yarýsýný geçince saati sýfýrla (24 -> 0)
        if (guncelSaat.Value >= 24f)
        {
            guncelSaat.Value = 0f;
            // Ýstersen burada "Yeni Gün Baþladý" event'i tetikleyebilirsin (Ekinleri büyütmek için)
        }
    }

    private void GorselleriGuncelle()
    {
        if (gunesIsigi == null) return;

        // MATEMATÝK: Saat 0 ile 24 arasýný, açý olarak 0 ile 360 arasýna çeviriyoruz.
        // -90 derece ekliyoruz çünkü saat 00:00'da güneþ tam altýmýzda (gece) olmalý.
        float gunesAcisi = (guncelSaat.Value / 24f) * 360f - 90f;

        // Güneþi X ekseninde döndür (Y eksenini hafif çapraz veriyoruz ki gölgeler düz düþmesin)
        gunesIsigi.transform.rotation = Quaternion.Euler(gunesAcisi, 170f, 0f);

        // Güneþin þiddetini AnimationCurve grafiðinden oku ve uygula
        gunesIsigi.intensity = gunesSiddeti.Evaluate(guncelSaat.Value);
    }
}