using UnityEngine;
using System.Collections;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    [Header("Çapa Ayarları")]
    public int fircaBoyutu = 3;
    public float harcananEnerji = 2f;
    public float maxVurusMesafesi = 2.5f;

    [Header("Görsel ve Ses Ayarları (Animasyon)")]
    public Vector3 vurusAcisi = new Vector3(65f, 0f, 0f);
    public float vurusSuresi = 0.4f;
    public AudioClip vurusSesi;
    public GameObject tozEfektiPrefab;

    private AudioSource audioSource;
    private Quaternion orijinalRotasyon;
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

        PlayerEnergy enerji = inventory.GetComponent<PlayerEnergy>();

        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Bu eylemi yapmak için yeterli enerjin yok.");
            return;
        }

        bool bosaSalladi = true;
        TerrainCollider tCol = null;

        if (hit.collider != null)
        {
            float mesafe = Vector3.Distance(inventory.transform.position, hit.point);

            if (mesafe <= maxVurusMesafesi)
            {
                if (hit.collider is TerrainCollider)
                {
                    bosaSalladi = false;
                    tCol = hit.collider as TerrainCollider;
                }
            }
            else
            {
                Debug.Log($"Çok uzaksın! Mesafe: {mesafe:F1}m");
            }
        }

        StartCoroutine(CapaVurAnimasyonu(hit, tCol, enerji, bosaSalladi, inventory));
    }

    private IEnumerator CapaVurAnimasyonu(RaycastHit hit, TerrainCollider tCol, PlayerEnergy enerji, bool bosaSalladi, PlayerInventory inventory)
    {
        isSwinging = true;

        Quaternion hedefRotasyon = orijinalRotasyon * Quaternion.Euler(vurusAcisi);

        float gecenZaman = 0f;
        float yariSure = vurusSuresi / 2f;

        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;

            transform.localRotation = Quaternion.Slerp(orijinalRotasyon, hedefRotasyon, t * t);
            yield return null;
        }

        transform.localRotation = hedefRotasyon;

        if (!bosaSalladi)
        {
            if (vurusSesi != null && audioSource != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(vurusSesi);
            }

            if (tozEfektiPrefab != null)
            {
                Vector3 dogumNoktasi = hit.point + (Vector3.up * 0.15f);
                GameObject toz = Instantiate(tozEfektiPrefab, dogumNoktasi, Quaternion.LookRotation(hit.normal));
                Destroy(toz, 2f);
            }

            var manager = tCol.GetComponent<TerrainLayerManager>();
            if (manager != null)
            {
                manager.PaintSoilServerRpc(hit.point, 1, fircaBoyutu);

                if (enerji != null)
                {
                    enerji.EnerjiHarcaServerRpc(harcananEnerji);
                }
            }

            if (inventory != null)
            {
                inventory.UseHeldToolServerRpc(1f);
            }
        }

        gecenZaman = 0f;
        while (gecenZaman < yariSure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / yariSure;

            transform.localRotation = Quaternion.Slerp(hedefRotasyon, orijinalRotasyon, t);
            yield return null;
        }

        transform.localRotation = orijinalRotasyon;
        isSwinging = false;
    }
}