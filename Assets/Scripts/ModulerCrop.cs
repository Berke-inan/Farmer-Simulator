using Unity.Netcode;
using UnityEngine;

public class ModularCrop : NetworkBehaviour
{
    public NetworkVariable<int> tohumID = new NetworkVariable<int>();
    public NetworkVariable<int> mevcutAsama = new NetworkVariable<int>(0);
    public NetworkVariable<bool> sulandiMi = new NetworkVariable<bool>(false);

    [Header("Büyüme Görselleri")]
    public GameObject[] asamaGorselleri;

    [Header("Çürüme Ayarları")]
    public float curumeSuresi = 120f;

    private TohumVerisi _veriler;
    private float _buyumeSayaci = 0f;
    private float _kurulukSayaci = 0f;

    [Header("Fertilizer Data")]
    public NetworkVariable<bool> isFertilized = new NetworkVariable<bool>(false);
    public NetworkVariable<float> growthTimeMultiplier = new NetworkVariable<float>(1f);
    public NetworkVariable<int> extraYield = new NetworkVariable<int>(0);

    // --- YENİ EKLENEN KISIM ---
    [Header("Pest Control Data")]
    public NetworkVariable<bool> ilaclandiMi = new NetworkVariable<bool>(false);
    // --------------------------

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
                float currentStageTime = _veriler.asamaGecisSuresi * growthTimeMultiplier.Value;

                if (_buyumeSayaci >= currentStageTime)
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

    public void ApplyFertilizer(float timeMultiplier, int bonus)
    {
        if (!IsServer) return;
        isFertilized.Value = true;
        growthTimeMultiplier.Value = timeMultiplier;
        extraYield.Value = bonus;
    }

    // --- YENİ EKLENEN RPC METODU ---
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]// İlaclamaMakinesi sahibi olmayan oyuncular da tetikleyebilsin diye false yaptık
    public void IlaclandiServerRpc()
    {
        ilaclandiMi.Value = true;
    }
}