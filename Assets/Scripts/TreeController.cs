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
        if (!IsServer || mevcutDurum.Value == TreeState.Kuru || !ilkSuVerildiMi) return;

        // 1. SUSUZLUK KRONOMETRESÝ
        susuzKalanSure += Time.deltaTime;
        if (susuzKalanSure >= agacVerisi.kurumaSiniri)
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

    // ==========================================
    // SEPET KODUNUN ÇAÐIRACAÐI HASAT FONKSÝYONU (Sadece Sunucuda Çalýþýr)
    // ==========================================
    public bool MeyveHasatEt()
    {
        if (mevcutDurum.Value == TreeState.Meyveli && agactakiMeyve.Value > 0)
        {
            agactakiMeyve.Value--; // Aðaçtaki meyveyi eksilt

            // Eðer aðaçta meyve bittiyse meyvesiz haline dön ve büyümeye baþtan baþlasýn
            if (agactakiMeyve.Value <= 0)
            {
                mevcutDurum.Value = TreeState.Buyumus;
                gecenBuyumeSuresi = 0f;
            }
            return true; // Baþarýyla toplandý
        }
        return false; // Toplanacak meyve yok
    }

    // Aðaca týklanýnca (E) sulanýr
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
}