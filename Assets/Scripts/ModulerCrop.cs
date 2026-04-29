using Unity.Netcode;
using UnityEngine;

public class ModularCrop : NetworkBehaviour
{
    public NetworkVariable<int> tohumID = new NetworkVariable<int>();
    public NetworkVariable<int> mevcutAsama = new NetworkVariable<int>(0);
    public NetworkVariable<bool> sulandiMi = new NetworkVariable<bool>(false);

    [Header("Büyüme Görselleri")]
    [Tooltip("Örn: 0:Fide, 1:Orta, 2:Büyük, 3:Çürümüş")]
    public GameObject[] asamaGorselleri;

    [Header("Çürüme Ayarları")]
    public float curumeSuresi = 120f; // Büyümüş bitki hasat edilmeden ne kadar susuz kalırsa çürür?

    private TohumVerisi _veriler;
    private float _buyumeSayaci = 0f;
    private float _kurulukSayaci = 0f;

    // Sağlıklı Büyümüş hal SONDAN BİR ÖNCEKİ index
    public bool IsGrown => asamaGorselleri != null && mevcutAsama.Value == asamaGorselleri.Length - 2;

    // Çürümüş hal EN SONDAKİ index
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

        // Bitki zaten çürümüşse artık hiçbir işlem yapma
        if (IsRotted) return;

        if (Time.frameCount % 30 == 0)
        {
            sulandiMi.Value = TerrainLayerManager.Instance.IsSoilWet(transform.position);
        }

        if (sulandiMi.Value)
        {
            _kurulukSayaci = 0f; // Toprak ıslaksa kuruluk sayacı sıfırlanır

            // Sadece sağlıklı büyümüş aşamaya gelene kadar büyü
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
            // TOPRAK KURU İSE:
            // Sadece ekin tam büyümüş (IsGrown) durumdaysa çürüme başlar
            if (IsGrown)
            {
                _kurulukSayaci += Time.deltaTime;
                if (_kurulukSayaci >= curumeSuresi)
                {
                    // Süre dolduğunda Çürümüş (Son Index) haline geç
                    mevcutAsama.Value = asamaGorselleri.Length - 1;
                }
            }
            // IsGrown değilse (fide vs. ise) hiçbir şey olmaz. 
            // Büyüme sayacı durur, çürüme sayacı da artmaz. Olduğu gibi bekler.
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
}