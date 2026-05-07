using Unity.Netcode;
using UnityEngine;

public class ModularCrop : NetworkBehaviour
{
    public NetworkVariable<int> tohumID = new NetworkVariable<int>();
    public NetworkVariable<int> mevcutAsama = new NetworkVariable<int>(0);
    public NetworkVariable<bool> sulandiMi = new NetworkVariable<bool>(false);

    [Header("İlaçlama Ayarları (YENİ)")]
    public NetworkVariable<bool> ilaclandiMi = new NetworkVariable<bool>(false);
    public float ilacDirenciCarpani = 2f; // İlaçlanınca çürüme süresi 2 kat uzar

    [Header("Büyüme Görselleri")]
    [Tooltip("Örn: 0:Fide, 1:Orta, 2:Büyük, 3:Çürümüş")]
    public GameObject[] asamaGorselleri;

    [Header("Çürüme Ayarları")]
    public float curumeSuresi = 120f;

    private TohumVerisi _veriler;
    private float _buyumeSayaci = 0f;
    private float _kurulukSayaci = 0f;

    public bool IsGrown => asamaGorselleri != null && mevcutAsama.Value == asamaGorselleri.Length - 2;
    public bool IsRotted => asamaGorselleri != null && mevcutAsama.Value == asamaGorselleri.Length - 1;

    public override void OnNetworkSpawn()
    {
        _veriler = TerrainLayerManager.Instance.GetTohumVerisi(tohumID.Value);
        mevcutAsama.OnValueChanged += (eski, yeni) => GorseliGuncelle();
        GorseliGuncelle();
    }

    void Update()
    {
        if (!IsServer || _veriler == null) return;
        if (IsRotted) return;

        if (Time.frameCount % 30 == 0)
        {
            sulandiMi.Value = TerrainLayerManager.Instance.IsSoilWet(transform.position);
        }

        if (sulandiMi.Value)
        {
            _kurulukSayaci = 0f;

            if (mevcutAsama.Value < asamaGorselleri.Length - 2)
            {
                _buyumeSayaci += Time.deltaTime;
                if (_buyumeSayaci >= _veriler.asamaGecisSuresi)
                {
                    _buyumeSayaci = 0f;
                    mevcutAsama.Value++;
                }
            }
        }
        else
        {
            if (IsGrown)
            {
                _kurulukSayaci += Time.deltaTime;
                if (_kurulukSayaci >= curumeSuresi)
                {
                    mevcutAsama.Value = asamaGorselleri.Length - 1;
                }
            }
        }
    }

    private void GorseliGuncelle()
    {
        if (asamaGorselleri == null) return;
        for (int i = 0; i < asamaGorselleri.Length; i++)
        {
            if (asamaGorselleri[i] != null)
                asamaGorselleri[i].SetActive(i == mevcutAsama.Value);
        }
    }

    // ==========================================
    // YENİ EKLENEN İLAÇLAMA FONKSİYONU
    // ==========================================
    [Rpc(SendTo.Server)]
    public void IlaclandiServerRpc()
    {
        // Çürümemişse ve henüz ilaçlanmamışsa
        if (!ilaclandiMi.Value && !IsRotted)
        {
            ilaclandiMi.Value = true;
            curumeSuresi *= ilacDirenciCarpani; // Kilit Nokta: Update kodunu yormamak için çürüme sınırını direkt 2 katına çıkarıyoruz!
            Debug.Log("Ekin ilaçlandı! Yeni çürüme süresi: " + curumeSuresi);
        }
    }
}