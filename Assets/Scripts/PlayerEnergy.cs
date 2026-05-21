using UnityEngine;
using Unity.Netcode;

public class PlayerEnergy : NetworkBehaviour
{
    [Header("Enerji Ayarlarý")]
    public float maksimumKapasite = 100f; // Asla geçilemeyecek üst sýnýr

    [Tooltip("Normal enerjiden 1 puan kaç saniyede bir düþsün?")]
    public float normalEnerjiDusmeSuresi = 10f;

    [Header("Maksimum Enerji Düþüþ Ayarlarý")]
    [Tooltip("Maksimum enerjinin düþmesi için harcanmasý/azalmasý gereken normal enerji (Örn: 5)")]
    public float maxEnerjiDusmeEsigi = 5f;

    [Tooltip("Eþik aþýldýðýnda maksimum enerjiden ne kadar düþülecek (Örn: 1)")]
    public float maxEnerjiDususMiktari = 1f;

    [Header("Yorgunluk Sýnýrlarý")]
    [Tooltip("Enerji bu deðerin altýndaysa alet kullanamaz (Örn: 5)")]
    public float eylemYapmaSiniri = 5f;

    [Tooltip("Enerji bu deðere veya altýna düþerse koþamaz (Örn: 0)")]
    public float kosmaSiniri = 0f;

    [Header("Günlük Limitler")]
    public int gunlukSuIcmeLimiti = 4;

    [Header("Canlý Deðerler (Að Senkronizeli)")]
    public NetworkVariable<float> maxEnerji = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> guncelEnerji = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> bugunIcilenSu = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Sayaçlar ve Birikim Deðiþkenleri
    private float normalSayac = 0f;
    private float harcananEnerjiBirikimi = 0f; // Max enerjiyi düþürmek için harcanan enerjiyi sayar

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DayNightCycleManager.YeniGunBasladiSinyali += YeniGunSifirlamasi;
            YeniGunSifirlamasi();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) DayNightCycleManager.YeniGunBasladiSinyali -= YeniGunSifirlamasi;
    }

    private void Update()
    {
        if (!IsServer) return;

        // NORMAL ENERJÝ DÜÞÜÞÜ
        if (guncelEnerji.Value > 0)
        {
            normalSayac += Time.deltaTime;
            if (normalSayac >= normalEnerjiDusmeSuresi)
            {
                guncelEnerji.Value -= 1f;
                normalSayac = 0f;

                // Zamanla azalan enerjiyi de birikime ekle
                EnerjiTuketimiKaydet(1f);
            }
        }

        // GÜVENLÝK: Normal enerji, anlýk Maksimum enerjiyi asla geçemez
        if (guncelEnerji.Value > maxEnerji.Value)
        {
            guncelEnerji.Value = maxEnerji.Value;
        }
    }

    // Aletleri kullanýnca (Çapa vb.) çaðrýlacak fonksiyon
    [Rpc(SendTo.Server)]
    public void EnerjiHarcaServerRpc(float harcanacakMiktar)
    {
        guncelEnerji.Value -= harcanacakMiktar;
        if (guncelEnerji.Value < 0) guncelEnerji.Value = 0;

        // Harcanan enerjiyi max enerji düþüþü için kaydet
        EnerjiTuketimiKaydet(harcanacakMiktar);

        Debug.Log($"Enerji Harcandý: {harcanacakMiktar}. Kalan: {guncelEnerji.Value}");
    }

    // Yemek ve Su eþyalarýnýn çaðýracaðý fonksiyon
    [Rpc(SendTo.Server)]
    public void TuketimYapServerRpc(bool suMu, float eklenecekMiktar)
    {
        if (suMu)
        {
            if (bugunIcilenSu.Value < gunlukSuIcmeLimiti)
            {
                bugunIcilenSu.Value++;
                maxEnerji.Value += eklenecekMiktar;

                if (maxEnerji.Value > maksimumKapasite) maxEnerji.Value = maksimumKapasite;

                Debug.Log($"Su Ýçildi! Max Enerji: {maxEnerji.Value} (Ýçilen: {bugunIcilenSu.Value}/{gunlukSuIcmeLimiti})");
            }
            else
            {
                Debug.Log("Bugünlük su içme limitine (4) ulaþtýn!");
            }
        }
        else
        {
            guncelEnerji.Value += eklenecekMiktar;

            if (guncelEnerji.Value > maxEnerji.Value) guncelEnerji.Value = maxEnerji.Value;

            Debug.Log($"Yemek Yendi! Enerji: {guncelEnerji.Value}");
        }
    }

    // --- YENÝ: Maksimum Enerji Düþüþünü Hesaplayan Fonksiyon ---
    private void EnerjiTuketimiKaydet(float miktar)
    {
        harcananEnerjiBirikimi += miktar;

        // Harcanan enerji belirlenen eþiði (Örn: 5) her geçtiðinde
        while (harcananEnerjiBirikimi >= maxEnerjiDusmeEsigi)
        {
            harcananEnerjiBirikimi -= maxEnerjiDusmeEsigi; // Eþiði birikimden çýkar
            maxEnerji.Value -= maxEnerjiDususMiktari;      // Max enerjiyi düþür

            if (maxEnerji.Value < 0) maxEnerji.Value = 0;

            Debug.Log($"Max enerji düþtü! Yeni Max: {maxEnerji.Value}");
        }
    }

    // Sabah olduðunda her þeyi fulleyen sistem
    private void YeniGunSifirlamasi()
    {
        maxEnerji.Value = maksimumKapasite;
        guncelEnerji.Value = maksimumKapasite;
        bugunIcilenSu.Value = 0;

        normalSayac = 0f;
        harcananEnerjiBirikimi = 0f; // Yeni günde yorgunluk birikimini sýfýrla
        Debug.Log("SABAH OLDU! Enerjiler 100'lendi, su limiti sýfýrlandý.");
    }

    public bool EylemYapabilirMi()
    {
        return guncelEnerji.Value >= eylemYapmaSiniri;
    }

    public bool KosabilirMi()
    {
        return guncelEnerji.Value > kosmaSiniri;
    }
}