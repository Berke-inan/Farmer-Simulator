using UnityEngine;
using Unity.Netcode;

public enum TreeState { Fide, Buyumus, Meyveli, Kuru }

public class TreeController : NetworkBehaviour, IInteractable
{
    [Header("Aðaç Verisi (Scriptable Object)")]
    public TreeData agacVerisi;

    [Header("Görsel Modeller")]
    public GameObject modelFide;
    public GameObject modelBuyumus;
    public GameObject modelMeyveli;
    public GameObject modelKuru;

    [Header("Canlý Durumlar")]
    public NetworkVariable<TreeState> mevcutDurum = new NetworkVariable<TreeState>(TreeState.Fide, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> agactakiMeyve = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Ýlaçlama Ayarlarý")]
    public NetworkVariable<bool> ilaclandiMi = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Tooltip("Ýlaçlanýrsa kuruma süresi kaç katýna çýksýn? (Örn: 2 = Ýki kat daha geç kurur)")]
    public float ilacDirenciCarpani = 2f;

    // Kronometreler
    private float gecenBuyumeSuresi = 0f;
    private float susuzKalanSure = 0f;
    private bool ilkSuVerildiMi = false;

    public override void OnNetworkSpawn()
    {
        mevcutDurum.OnValueChanged += DurumDegistigindeGorselleriGuncelle;
        GorselleriUygula(mevcutDurum.Value);
    }

    public override void OnNetworkDespawn()
    {
        mevcutDurum.OnValueChanged -= DurumDegistigindeGorselleriGuncelle;
    }

    private void DurumDegistigindeGorselleriGuncelle(TreeState eskiDurum, TreeState yeniDurum)
    {
        GorselleriUygula(yeniDurum);
    }

    private void GorselleriUygula(TreeState durum)
    {
        if (modelFide != null) modelFide.SetActive(durum == TreeState.Fide);
        if (modelBuyumus != null) modelBuyumus.SetActive(durum == TreeState.Buyumus);
        if (modelMeyveli != null) modelMeyveli.SetActive(durum == TreeState.Meyveli);
        if (modelKuru != null) modelKuru.SetActive(durum == TreeState.Kuru);
    }

    private void Update()
    {
        // Kuruysa veya Server deðilse hiç iþlem yapma
        if (!IsServer || mevcutDurum.Value == TreeState.Kuru) return;

        // =========================================================
        // YENÝ EKLENEN KISIM: Ekinler gibi topraðý kontrol et!
        // =========================================================
        if (Time.frameCount % 30 == 0) // Her 30 karede bir (Performans dostu)
        {
            if (TerrainLayerManager.Instance != null && TerrainLayerManager.Instance.IsSoilWet(transform.position))
            {
                // Toprak ýslaksa, otomatik olarak suyu içmiþ say
                ilkSuVerildiMi = true;
                susuzKalanSure = 0f;
            }
        }

        // Eðer hala ilk suyunu almadýysa (toprak kuruysa ve kovayla sulanmadýysa), büyümeyi durdur
        if (!ilkSuVerildiMi) return;
        // =========================================================


        // 1. SUSUZLUK KRONOMETRESÝ
        susuzKalanSure += Time.deltaTime;

        // --- ÝLAÇLAMA SÜRE UZATMA MANTIÐI ---
        float mevcutKurumaSiniri = agacVerisi.kurumaSiniri;
        if (ilaclandiMi.Value)
        {
            mevcutKurumaSiniri *= ilacDirenciCarpani;
        }

        if (susuzKalanSure >= mevcutKurumaSiniri)
        {
            mevcutDurum.Value = TreeState.Kuru;
            return;
        }

        // 2. BÜYÜME KRONOMETRESÝ
        gecenBuyumeSuresi += Time.deltaTime;

        if (mevcutDurum.Value == TreeState.Fide && gecenBuyumeSuresi >= agacVerisi.buyumeSuresi)
        {
            mevcutDurum.Value = TreeState.Buyumus;
            gecenBuyumeSuresi = 0f;
        }
        else if (mevcutDurum.Value == TreeState.Buyumus && gecenBuyumeSuresi >= agacVerisi.meyveVermeSuresi)
        {
            mevcutDurum.Value = TreeState.Meyveli;
            agactakiMeyve.Value = 2; // Aðaç meyve verdiðinde üstünde 2 hasatlýk meyve olur
            gecenBuyumeSuresi = 0f;
        }
    }

    // Sepet kodu burayý çaðýrýr
    public bool MeyveHasatEt()
    {
        if (mevcutDurum.Value == TreeState.Meyveli && agactakiMeyve.Value > 0)
        {
            agactakiMeyve.Value--;
            if (agactakiMeyve.Value <= 0)
            {
                mevcutDurum.Value = TreeState.Buyumus;
                gecenBuyumeSuresi = 0f;
            }
            return true;
        }
        return false;
    }

    // Sulama
    public void Interact(NetworkObject interactor)
    {
        if (mevcutDurum.Value == TreeState.Kuru) return;
        SulamaYapServerRpc();
    }

    [Rpc(SendTo.Server)]
    public void SulamaYapServerRpc()
    {
        if (mevcutDurum.Value != TreeState.Kuru)
        {
            ilkSuVerildiMi = true;
            susuzKalanSure = 0f;
        }
    }

    // ==========================================
    // ÝLAÇLAMA FONKSÝYONU
    // ==========================================
    [Rpc(SendTo.Server)]
    public void IlaclandiServerRpc()
    {
        if (!ilaclandiMi.Value && mevcutDurum.Value != TreeState.Kuru)
        {
            ilaclandiMi.Value = true;
            Debug.Log($"Aðaç baþarýyla ilaçlandý! Yeni kuruma sýnýrý: {agacVerisi.kurumaSiniri * ilacDirenciCarpani} saniye oldu.");
        }
    }
}