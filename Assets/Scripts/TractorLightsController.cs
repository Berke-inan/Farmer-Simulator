using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TractorController))] // Işık scripti, traktör scriptinin varlığından emin olur
public class TractorLightsController : NetworkBehaviour
{
    [Header("Işık Objeleri")]
    [Tooltip("L tuşu ile açılacak ön farlar")]
    public GameObject[] onFarlar;

    [Tooltip("Frene basıldığında yanacak kırmızı ışıklar")]
    public GameObject[] frenLambalari;

    [Tooltip("Geri geri giderken yanacak beyaz ışıklar")]
    public GameObject[] geriLambalari;

    [Header("Ağ Senkronizasyonu (Elle Dokunma)")]
    public NetworkVariable<bool> isHeadlightsOn = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isBraking = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isReversing = new NetworkVariable<bool>(false);

    private Rigidbody rb;

    // --- SENİN FİKRİN: Ana Kontrolcüyü (TractorController) Referans Alıyoruz ---
    private TractorController anaKontrolcu;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Aynı objenin üzerindeki senin kendi yazdığın TractorController scriptini bulur
        anaKontrolcu = GetComponent<TractorController>();
    }

    public override void OnNetworkSpawn()
    {
        isHeadlightsOn.OnValueChanged += (eski, yeni) => IklariGuncelle(onFarlar, yeni);
        isBraking.OnValueChanged += (eski, yeni) => IklariGuncelle(frenLambalari, yeni);
        isReversing.OnValueChanged += (eski, yeni) => IklariGuncelle(geriLambalari, yeni);

        IklariGuncelle(onFarlar, isHeadlightsOn.Value);
        IklariGuncelle(frenLambalari, isBraking.Value);
        IklariGuncelle(geriLambalari, isReversing.Value);
    }

    private void Update()
    {
        // --- SENİN KUSURSUZ MANTIĞIN ---
        // "Benim ana kontrolcümde 'IsDrivenByMe' (Ben mi Sürüyorum?) değeri True değilse, tuşları dinleme!"
        if (anaKontrolcu == null || !anaKontrolcu.IsDrivenByMe) return;

        // --- 1. ÖN FAR KONTROLÜ (L Tuşu) ---
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            FarlariAcKapatServerRpc();
        }

        // --- 2. HIZ VE YÖN HESAPLAMASI ---
        float yerelHizZ = transform.InverseTransformDirection(rb.linearVelocity).z;
        bool wBasili = Keyboard.current != null && Keyboard.current.wKey.isPressed;
        bool sBasili = Keyboard.current != null && Keyboard.current.sKey.isPressed;

        // --- 3. FREN KONTROLÜ ---
        bool suAnFrenYapiliyor = false;
        if (yerelHizZ > 0.5f && sBasili) suAnFrenYapiliyor = true;
        else if (yerelHizZ < -0.5f && wBasili) suAnFrenYapiliyor = true;

        if (isBraking.Value != suAnFrenYapiliyor)
        {
            FrenDurumunuAyarlaServerRpc(suAnFrenYapiliyor);
        }

        // --- 4. GERİ VİTES KONTROLÜ ---
        bool suAnGeriGidiyor = yerelHizZ < -0.1f;

        if (isReversing.Value != suAnGeriGidiyor)
        {
            GeriVitesDurumunuAyarlaServerRpc(suAnGeriGidiyor);
        }
    }

    [Rpc(SendTo.Server)]
    private void FarlariAcKapatServerRpc()
    {
        isHeadlightsOn.Value = !isHeadlightsOn.Value;
    }

    [Rpc(SendTo.Server)]
    private void FrenDurumunuAyarlaServerRpc(bool durum)
    {
        isBraking.Value = durum;
    }

    [Rpc(SendTo.Server)]
    private void GeriVitesDurumunuAyarlaServerRpc(bool durum)
    {
        isReversing.Value = durum;
    }

    private void IklariGuncelle(GameObject[] isikListesi, bool acikMi)
    {
        foreach (var isik in isikListesi)
        {
            if (isik != null)
            {
                isik.SetActive(acikMi);
            }
        }
    }
}