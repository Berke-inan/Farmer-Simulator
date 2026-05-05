using UnityEngine;
using Unity.Netcode;

public class PlayerEnergy : NetworkBehaviour
{
    [Header("Enerji Ayarlarý")]
    public float maksimumKapasite = 100f; // Asla geçilemeyecek üst sýnýr

    [Tooltip("Normal enerjiden 1 puan kaç saniyede bir düþsün?")]
    public float normalEnerjiDusmeSuresi = 10f;

    [Tooltip("Maksimum enerjiden 1 puan kaç saniyede bir düþsün?")]
    public float maxEnerjiDusmeSuresi = 30f;

    [Header("Günlük Limitler")]
    public int gunlukSuIcmeLimiti = 4;

    [Header("Canlý Deðerler (Að Senkronizeli)")]
    public NetworkVariable<float> maxEnerji = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> guncelEnerji = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> bugunIcilenSu = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Saniye sayýcýlar
    private float normalSayac = 0f;
    private float maxSayac = 0f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Oyuncu doðduðunda zaman yöneticisindeki sabah sinyaline abone ol
            DayNightCycleManager.YeniGunBasladiSinyali += YeniGunSifirlamasi;

            // Oyuna ilk giriþte enerjileri fulle
            YeniGunSifirlamasi();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) DayNightCycleManager.YeniGunBasladiSinyali -= YeniGunSifirlamasi;
    }

    private void Update()
    {
        // Enerji düþüþünü hileleri önlemek için sadece Sunucu hesaplar
        if (!IsServer) return;

        // 1. MAKSÝMUM ENERJÝ DÜÞÜÞÜ
        if (maxEnerji.Value > 0)
        {
            maxSayac += Time.deltaTime;
            if (maxSayac >= maxEnerjiDusmeSuresi)
            {
                maxEnerji.Value -= 1f;
                maxSayac = 0f;
            }
        }

        // 2. NORMAL ENERJÝ DÜÞÜÞÜ
        if (guncelEnerji.Value > 0)
        {
            normalSayac += Time.deltaTime;
            if (normalSayac >= normalEnerjiDusmeSuresi)
            {
                guncelEnerji.Value -= 1f;
                normalSayac = 0f;
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

        Debug.Log($"Enerji Harcandý: {harcanacakMiktar}. Kalan: {guncelEnerji.Value}");
    }

    // Yemek ve Su eþyalarýnýn çaðýracaðý fonksiyon
    [Rpc(SendTo.Server)]
    public void TuketimYapServerRpc(bool suMu, float eklenecekMiktar)
    {
        if (suMu)
        {
            // SU ÝÇÝLÝRSE: Maksimum enerjiyi artýrýr (Limiti aþmadýysa)
            if (bugunIcilenSu.Value < gunlukSuIcmeLimiti)
            {
                bugunIcilenSu.Value++;
                maxEnerji.Value += eklenecekMiktar;

                // Kapasiteyi(100) geçmesini engelle
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
            // YEMEK YENÝRSE: Normal enerjiyi artýrýr
            guncelEnerji.Value += eklenecekMiktar;

            // O anki maksimum enerjiyi geçmesini engelle
            if (guncelEnerji.Value > maxEnerji.Value) guncelEnerji.Value = maxEnerji.Value;

            Debug.Log($"Yemek Yendi! Enerji: {guncelEnerji.Value}");
        }
    }

    // Sabah olduðunda her þeyi fulleyen sistem
    private void YeniGunSifirlamasi()
    {
        maxEnerji.Value = maksimumKapasite;
        guncelEnerji.Value = maksimumKapasite;
        bugunIcilenSu.Value = 0;

        normalSayac = 0f;
        maxSayac = 0f;
        Debug.Log("SABAH OLDU! Enerjiler 100'lendi, su limiti sýfýrlandý.");
    }
}