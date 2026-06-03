using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class IngilizAnahtari : MonoBehaviour, IUseableTool
{
    public float beklemeSuresi = 0.5f;
    public AudioClip tamirSesi;

    [Header("Animasyon Ayarlarý")]
    [Tooltip("Anahtarýn hangi yöne büküleceði (X, Y veya Z ekseninde deneyerek bulabilirsin)")]
    public Vector3 donusAcisi = new Vector3(0f, 0f, 45f);
    public float animasyonSuresi = 0.3f;

    private float sonVurusZamani;
    private Quaternion orijinalRotasyon;
    private bool isAnimating = false;

    private void Awake()
    {
        // Anahtar ilk doðduðunda eldeki duruþunu kaydet
        orijinalRotasyon = transform.localRotation;
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // Bekleme süresi dolmadýysa veya zaten animasyon oynuyorsa iptal et
        if (Time.time - sonVurusZamani < beklemeSuresi || isAnimating) return;
        sonVurusZamani = Time.time;

        // Vurduðumuz yerde bozuk parça olmasa bile havaya sallama animasyonu oynasýn
        StartCoroutine(BilekHareketiAnimasyonu());

        RepairableTarget[] hedefler = hit.collider.transform.root.GetComponentsInChildren<RepairableTarget>();

        foreach (RepairableTarget hedef in hedefler)
        {
            BreakDisableBehavior bozulma = hedef.GetComponent<BreakDisableBehavior>();
            if (bozulma != null && bozulma.isBroken.Value)
            {
                if (tamirSesi != null) AudioSource.PlayClipAtPoint(tamirSesi, hit.point);
                hedef.ReceiveRepairHit();
                break;
            }
        }
    }

    private IEnumerator BilekHareketiAnimasyonu()
    {
        isAnimating = true;
        Quaternion hedefRotasyon = orijinalRotasyon * Quaternion.Euler(donusAcisi);

        float gecenZaman = 0f;
        float yariSure = animasyonSuresi / 2f;

        // 1. AÞAMA: Somunu sýkma (Bükülme)
        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;
            transform.localRotation = Quaternion.Slerp(orijinalRotasyon, hedefRotasyon, t);
            yield return null;
        }

        // 2. AÞAMA: Geri çekilme (Orijinal konuma dönüþ)
        gecenZaman = 0f;
        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;
            transform.localRotation = Quaternion.Slerp(hedefRotasyon, orijinalRotasyon, t);
            yield return null;
        }

        // Sapmalarý önlemek için son rotasyonu zorla sabitle
        transform.localRotation = orijinalRotasyon;
        isAnimating = false;
    }
}