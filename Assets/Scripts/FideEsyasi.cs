using UnityEngine;
using Unity.Netcode;

public class FideEsyasi : MonoBehaviour, IUseableTool
{
    [Header("Ekim Ayarlarý")]
    [Tooltip("Inspector'dan LemonTreeMaster prefabýný buraya sürüklemeyi unutma!")]
    public GameObject agacPrefab;
    public float minimumEkimMesafesi = 1.0f;
    public float ekimMenzili = 4f;

    [Header("Efektler")]
    public AudioClip ekmeSesi;
    public GameObject tozEfektiPrefab;

    private AudioSource audioSource;
    private float sonEkmeZamani = 0f;
    private float ekmeBeklemeSuresi = 0.5f;

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
        if (Time.time - sonEkmeZamani < ekmeBeklemeSuresi) return;
        if (hit.collider == null) return;

        if (Vector3.Distance(inventory.transform.position, hit.point) > ekimMenzili)
        {
            Debug.Log("Ekmek için çok uzaksýn!");
            return;
        }

        if (hit.collider is TerrainCollider tCol)
        {
            TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();

            if (manager != null && manager.IsSoilTilled(hit.point))
            {
                if (!YakinlardaBitkiVarMi(hit.point))
                {
                    EkmeyiGerceklestir(hit.point, hit.normal, inventory);
                }
                else
                {
                    Debug.Log("Buraya çok yakýn baþka bir bitki veya aðaç var!");
                }
            }
            else
            {
                Debug.Log("Buradaki toprak sürülmemiþ! Önce çapalamalýsýn.");
            }
        }
    }

    private void EkmeyiGerceklestir(Vector3 pozisyon, Vector3 normal, PlayerInventory inventory)
    {
        sonEkmeZamani = Time.time;

        // --- GÖRSEL VE SES EFEKTLERÝ ---
        if (ekmeSesi != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(ekmeSesi);
        }

        if (tozEfektiPrefab != null)
        {
            Vector3 efektPozisyonu = pozisyon + (Vector3.up * 0.15f);
            GameObject toz = Instantiate(tozEfektiPrefab, efektPozisyonu, Quaternion.LookRotation(normal));
            Destroy(toz, 2f);
        }

        // --- 1. ADIM: AÐACI DÝK (Garantili Çalýþan Yöntem) ---
        if (NetworkManager.Singleton.IsServer)
        {
            GameObject yeniAgac = Instantiate(agacPrefab, pozisyon, Quaternion.identity);
            yeniAgac.GetComponent<NetworkObject>().Spawn();
        }
        else
        {
            inventory.DikmeIstegiServerRpc(pozisyon, inventory.activeHotbarIndex.Value);
        }

        // --- 2. ADIM: ENVANTERDEN TÜKET (Hayalet Slotu Çözen Kýsým) ---
        // Zamanlama çakýþmasý olmamasý için tüketim iþlemini aðaç yaratýldýktan hemen sonra yapýyoruz
        inventory.EldeTuketimYapServerRpc();
    }

    private bool YakinlardaBitkiVarMi(Vector3 nokta)
    {
        Collider[] yakindakiler = Physics.OverlapSphere(nokta, minimumEkimMesafesi);
        foreach (var col in yakindakiler)
        {
            if (col.GetComponent<ModularCrop>() != null || col.GetComponent<TreeController>() != null)
            {
                return true;
            }
        }
        return false;
    }
}