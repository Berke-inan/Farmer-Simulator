using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SprinkleController : NetworkBehaviour
{
    private Rigidbody rb;

    [Header("Görsel ve Ses (Sulamadan Bağımsız)")]
    public ParticleSystem suEfekti;
    public Transform donenBaslik;
    public AudioSource fiskiyeSesi;

    // TerrainCollider'ı sildik, çünkü artık sürükle bırak yapmayacağız!
    private TerrainLayerManager layerManager;

    [Header("Sulama Ayarları")]
    [Tooltip("Animasyon 1 turunu kaç saniyede tamamlıyorsa onu yaz (Örn: 7.8)")]
    public float sulamaAraligi = 7.8f;
    [Tooltip("Merkezi fıskiye olan boyama çemberinin çapı")]
    public int fircaBoyutu = 30;

    public NetworkVariable<bool> calisiyorMu = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float zamanlayici;
    private Coroutine animasyonKorutini;

    void Start()
    {
        // --- ÇÖZÜM BURADA: OTOMATİK BULMA SİSTEMİ ---
        // Fıskiye yere konduğu an sahnedeki TerrainLayerManager'ı otomatik bulur ve kilitler. Asla referans kaybetmez!
        layerManager = TerrainLayerManager.Instance;

        // Eğer Instance boş dönerse (ki bazen multiplayer'da olur), yedek plan olarak sahnede zorla arar:
        if (layerManager == null)
        {
            layerManager = FindObjectOfType<TerrainLayerManager>();
        }

        if (layerManager == null)
        {
            Debug.LogError("Sahnedeki Terrain (TerrainLayerManager) bulunamadı! Lütfen tarlada kodun olduğundan emin ol.");
        }
    }

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        if (donenBaslik == null) donenBaslik = transform;

        if (BoruVanasi.Instance != null)
        {
            BoruVanasi.Instance.anaSuAcikMi.OnValueChanged += AnaSuDurumuDegisti;
            if (BoruVanasi.Instance.anaSuAcikMi.Value && EklendiMi()) SuyuAc();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (BoruVanasi.Instance != null)
            BoruVanasi.Instance.anaSuAcikMi.OnValueChanged -= AnaSuDurumuDegisti;
    }

    private bool EklendiMi()
    {
        if (rb != null) return rb.isKinematic;
        return true;
    }

    private void AnaSuDurumuDegisti(bool eskiDurum, bool yeniDurum)
    {
        if (yeniDurum && EklendiMi()) SuyuAc();
        else SuyuKapat();
    }

    private void SuyuAc()
    {
        if (IsServer) calisiyorMu.Value = true;
        if (suEfekti != null) suEfekti.Play();

        zamanlayici = sulamaAraligi;

        if (animasyonKorutini == null) animasyonKorutini = StartCoroutine(FiskiyeAnimasyonDongusu());
    }

    private void SuyuKapat()
    {
        if (IsServer) calisiyorMu.Value = false;
        if (suEfekti != null) suEfekti.Stop();
        if (fiskiyeSesi != null) fiskiyeSesi.Stop();

        if (animasyonKorutini != null)
        {
            StopCoroutine(animasyonKorutini);
            animasyonKorutini = null;
        }
    }

    void Update()
    {
        if (!IsServer || !calisiyorMu.Value) return;

        zamanlayici += Time.deltaTime;

        if (zamanlayici >= sulamaAraligi)
        {
            SulamaIslemi();
            zamanlayici = 0f;
        }
    }

    private void SulamaIslemi()
    {
        if (layerManager == null) return;

        Vector3 sulamaPozisyonu = transform.position;
        layerManager.PaintSoilServerRpc(sulamaPozisyonu, layerManager.wetLayerIndex, fircaBoyutu);
    }

    private IEnumerator FiskiyeAnimasyonDongusu()
    {
        Vector3 baslangicRot = donenBaslik.localEulerAngles;

        while (true)
        {
            if (suEfekti != null && !suEfekti.isPlaying)
            {
                suEfekti.Play();
            }

            if (fiskiyeSesi != null) fiskiyeSesi.pitch = 1.0f;

            int adimSayisi = 24;
            float adimAcisi = 360f / adimSayisi;
            float beklemeSuresi = 0.2f;

            for (int i = 1; i <= adimSayisi; i++)
            {
                if (fiskiyeSesi != null) fiskiyeSesi.Play();
                donenBaslik.localRotation = Quaternion.Euler(baslangicRot.x, baslangicRot.y + (i * adimAcisi), baslangicRot.z);

                yield return new WaitForSeconds(beklemeSuresi);
            }

            if (fiskiyeSesi != null)
            {
                fiskiyeSesi.pitch = 1.4f;
                fiskiyeSesi.Play();
            }

            float gecenZaman = 0f;
            float donusSuresi = 2.5f;

            while (gecenZaman < donusSuresi)
            {
                gecenZaman += Time.deltaTime;
                float anlikAci = Mathf.Lerp(360f, 0f, gecenZaman / donusSuresi);

                donenBaslik.localRotation = Quaternion.Euler(baslangicRot.x, baslangicRot.y + anlikAci, baslangicRot.z);
                yield return null;
            }

            donenBaslik.localRotation = Quaternion.Euler(baslangicRot.x, baslangicRot.y, baslangicRot.z);
            yield return new WaitForSeconds(0.5f);
        }
    }
}