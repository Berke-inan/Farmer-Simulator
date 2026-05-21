using UnityEngine;
using System.Collections;

public class BaltaEylemi : MonoBehaviour, IUseableTool
{
    [Header("Balta Ayarlarý")]
    [Tooltip("Her vuruþta harcanacak enerji miktarý")]
    public float harcananEnerji = 2f;

    [Tooltip("Baltanýn aðaca ulaþabilmesi için maksimum mesafe (Metre)")]
    public float maxVurusMesafesi = 2.5f;

    [Header("Görsel ve Ses Ayarlarý (Animasyon)")]
    public float toplamAnimasyonSuresi = 0.5f;

    [Tooltip("Vuruþ öncesi saða doðru gerilme açýsý")]
    public Vector3 hazirlikAcisi = new Vector3(15f, 30f, 0f);
    [Tooltip("Sola ve aþaðý doðru çapraz sert balta vuruþu açýsý")]
    public Vector3 vurusAcisi = new Vector3(20f, -60f, 0f);

    [Tooltip("Baltanýn kol hareketi geniþliði")]
    public float hareketSiddeti = 0.2f;

    [Header("Efektler")]
    public AudioClip vurusSesi;
    [Tooltip("Aðaca vurunca fýrlayacak odun parçalarý (Particle Prefab)")]
    public GameObject odunParcasiEfektiPrefab;

    [Tooltip("Odun parçalarýnýn sana doðru deðil, saða/sola sýçrama açýsý (Y=45 saða atar)")]
    public Vector3 parcaSapmaAcisi = new Vector3(0f, 45f, 0f); // YENÝ EKLENDÝ

    private AudioSource audioSource;
    private Quaternion orijinalRotasyon;
    private Vector3 orijinalPozisyon;
    private bool isSwinging = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (isSwinging) return;

        orijinalRotasyon = transform.localRotation;
        orijinalPozisyon = transform.localPosition;

        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();
        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Balta sallayacak halin kalmadý.");
            return;
        }

        KesilebilirNesne hedefNesne = null;
        bool mesafedeMi = false;

        if (hit.collider != null)
        {
            float mesafe = Vector3.Distance(inventory.transform.position, hit.point);
            if (mesafe <= maxVurusMesafesi)
            {
                mesafedeMi = true;
                hit.collider.TryGetComponent(out hedefNesne);
            }
        }

        StartCoroutine(BaltaVurAnimasyonu(hit, hedefNesne, enerji, mesafedeMi));
    }

    private IEnumerator BaltaVurAnimasyonu(RaycastHit hit, KesilebilirNesne hedefNesne, PlayerEnergy enerji, bool mesafedeMi)
    {
        isSwinging = true;

        Vector3 geriPos = orijinalPozisyon + new Vector3(0.5f, 0.2f, -0.5f) * hareketSiddeti;
        Quaternion geriRot = orijinalRotasyon * Quaternion.Euler(hazirlikAcisi);

        Vector3 vurusPos = orijinalPozisyon + new Vector3(-0.8f, -0.4f, 0.8f) * hareketSiddeti;
        Quaternion vurusRot = orijinalRotasyon * Quaternion.Euler(vurusAcisi);

        yield return StartCoroutine(HareketEt(orijinalPozisyon, geriPos, orijinalRotasyon, geriRot, toplamAnimasyonSuresi * 0.30f));
        yield return StartCoroutine(HareketEt(geriPos, vurusPos, geriRot, vurusRot, toplamAnimasyonSuresi * 0.20f));

        if (mesafedeMi && hedefNesne != null)
        {
            if (vurusSesi != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.85f, 1.15f);
                audioSource.PlayOneShot(vurusSesi);
            }

            if (odunParcasiEfektiPrefab != null)
            {
                // YENÝ ÇÖZÜM: Direkt normali kullanmak yerine açýyý saða doðru büküyoruz
                Quaternion yuzeyYonu = Quaternion.LookRotation(hit.normal);
                Quaternion sagaDogruSapanYon = yuzeyYonu * Quaternion.Euler(parcaSapmaAcisi);

                GameObject parçalar = Instantiate(odunParcasiEfektiPrefab, hit.point, sagaDogruSapanYon);
                Destroy(parçalar, 1.5f);
            }

            hedefNesne.VurusAl();

            if (enerji != null)
            {
                enerji.EnerjiHarcaServerRpc(harcananEnerji);
            }
        }

        yield return StartCoroutine(HareketEt(vurusPos, orijinalPozisyon, vurusRot, orijinalRotasyon, toplamAnimasyonSuresi * 0.50f));

        transform.localPosition = orijinalPozisyon;
        transform.localRotation = orijinalRotasyon;
        isSwinging = false;
    }

    private IEnumerator HareketEt(Vector3 baslangicPos, Vector3 hedefPos, Quaternion baslangicRot, Quaternion hedefRot, float sure)
    {
        float gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            transform.localPosition = Vector3.Lerp(baslangicPos, hedefPos, smoothT);
            transform.localRotation = Quaternion.Slerp(baslangicRot, hedefRot, smoothT);

            yield return null;
        }
    }
}