using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ArsaManager : NetworkBehaviour
{
    public static ArsaManager Instance;

    [Header("Arsa Ayarlarý")]
    public float parselBoyutu = 10f;
    public GameObject citPrefab;

    [Header("Baþlangýç Çiftliði Ayarlarý")]
    public float baslangicGenislikMetre = 160f;
    public float baslangicUzunlukMetre = 160f;

    private NetworkList<Vector2Int> satinAlinanArsalar;
    private HashSet<Vector2Int> localSatinAlinanArsalar = new HashSet<Vector2Int>();
    private List<GameObject> aktifCitler = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        satinAlinanArsalar = new NetworkList<Vector2Int>(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        satinAlinanArsalar.OnListChanged += OnArsaListesiDegisti;

        if (IsServer)
        {
            BaslangicArazisiniOlustur();
        }
    }

    private void OnArsaListesiDegisti(NetworkListEvent<Vector2Int> changeEvent)
    {
        localSatinAlinanArsalar.Clear();
        foreach (var arsa in satinAlinanArsalar)
        {
            localSatinAlinanArsalar.Add(arsa);
        }
    }

    private void BaslangicArazisiniOlustur()
    {
        int xParselSayisi = Mathf.RoundToInt(baslangicGenislikMetre / parselBoyutu);
        int zParselSayisi = Mathf.RoundToInt(baslangicUzunlukMetre / parselBoyutu);

        int minX = -xParselSayisi / 2;
        int maxX = minX + xParselSayisi;
        int minZ = -zParselSayisi / 2;
        int maxZ = minZ + zParselSayisi;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                Vector2Int koordinat = new Vector2Int(x, z);
                if (!satinAlinanArsalar.Contains(koordinat))
                {
                    satinAlinanArsalar.Add(koordinat);
                }
            }
        }

        localSatinAlinanArsalar.Clear();
        foreach (var arsa in satinAlinanArsalar)
        {
            localSatinAlinanArsalar.Add(arsa);
        }

        CitleriYenidenCiz();
    }

    public Vector2Int PozisyonuGridKoordinatinaCevir(Vector3 pozisyon)
    {
        int x = Mathf.RoundToInt(pozisyon.x / parselBoyutu);
        int z = Mathf.RoundToInt(pozisyon.z / parselBoyutu);
        return new Vector2Int(x, z);
    }

    [Rpc(SendTo.Server)]
    public void ArsaSatinAlServerRpc(Vector3 tiklananPozisyon)
    {
        Vector2Int gridKoordinati = PozisyonuGridKoordinatinaCevir(tiklananPozisyon);

        if (!localSatinAlinanArsalar.Contains(gridKoordinati))
        {
            ArsayiSistemeEkle(gridKoordinati);
        }
    }

    private void ArsayiSistemeEkle(Vector2Int gridKoordinati)
    {
        satinAlinanArsalar.Add(gridKoordinati);
        localSatinAlinanArsalar.Add(gridKoordinati);
        CitleriYenidenCiz();
    }

    private void CitleriYenidenCiz()
    {
        if (!IsServer) return;

        foreach (GameObject cit in aktifCitler)
        {
            if (cit != null)
            {
                NetworkObject no = cit.GetComponent<NetworkObject>();
                if (no != null && no.IsSpawned) no.Despawn();
                else Destroy(cit);
            }
        }
        aktifCitler.Clear();

        foreach (Vector2Int arsa in localSatinAlinanArsalar)
        {
            Vector2Int[] komsular = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            for (int i = 0; i < komsular.Length; i++)
            {
                Vector2Int komsuKoordinat = arsa + komsular[i];
                if (!localSatinAlinanArsalar.Contains(komsuKoordinat))
                {
                    CitOlustur(arsa, komsular[i]);
                }
            }
        }
    }

    // Arazinin herhangi bir X,Z noktasýndaki gerçek yüksekliðini bulur
    private float GetZeminYuksekligi(Vector3 pozisyon)
    {
        if (Terrain.activeTerrain != null)
        {
            return Terrain.activeTerrain.SampleHeight(pozisyon) + Terrain.activeTerrain.transform.position.y;
        }

        Ray ray = new Ray(new Vector3(pozisyon.x, 500f, pozisyon.z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return hit.point.y;
        }
        return 0f;
    }

    private void CitOlustur(Vector2Int merkezArsa, Vector2Int yon)
    {
        float merkezX = merkezArsa.x * parselBoyutu;
        float merkezZ = merkezArsa.y * parselBoyutu;

        // Çitin 2D dünyadaki tam orta noktasý
        Vector3 merkez2D = new Vector3(merkezX, 0, merkezZ) + new Vector3(yon.x * (parselBoyutu / 2f), 0, yon.y * (parselBoyutu / 2f));

        Vector3 baslangicNoktasi = Vector3.zero;
        Vector3 bitisNoktasi = Vector3.zero;

        // Çitin uzandýðý iki ucu (Baþlangýç ve Bitiþ) hesaplýyoruz
        if (yon.y != 0) // Çit Doðu-Batý ekseninde (X Ekseni) uzanýyor
        {
            baslangicNoktasi = merkez2D + new Vector3(-parselBoyutu / 2f, 0, 0);
            bitisNoktasi = merkez2D + new Vector3(parselBoyutu / 2f, 0, 0);
        }
        else if (yon.x != 0) // Çit Kuzey-Güney ekseninde (Z Ekseni) uzanýyor
        {
            baslangicNoktasi = merkez2D + new Vector3(0, 0, -parselBoyutu / 2f);
            bitisNoktasi = merkez2D + new Vector3(0, 0, parselBoyutu / 2f);
        }

        // Ýki ucun da arazideki Y yüksekliklerini ayrý ayrý alýyoruz
        baslangicNoktasi.y = GetZeminYuksekligi(baslangicNoktasi);
        bitisNoktasi.y = GetZeminYuksekligi(bitisNoktasi);

        // Çitin yerleþeceði asýl pozisyon, bu eðimli iki noktanýn tam ortasýdýr
        Vector3 citPozisyonu = (baslangicNoktasi + bitisNoktasi) / 2f;

        // --- SÝHÝRLÝ KISIM: EÐÝME GÖRE AÇI HESAPLAMA ---
        // Çitin baþlangýcýndan bitiþine doðru giden vektörü buluyoruz
        Vector3 citYonu = (bitisNoktasi - baslangicNoktasi).normalized;

        // Vektör matematiði ile çitin arazinin eðimine tam yatmasýný saðlýyoruz
        Vector3 forwardVektoru = Vector3.Cross(citYonu, Vector3.up).normalized;
        Vector3 gercekUpVektoru = Vector3.Cross(forwardVektoru, citYonu).normalized;

        // Çit modelimizin ana uzantýsý yerel X ekseni olduðu için rotasyonu ona göre diziyoruz
        Quaternion citRotasyonu = Quaternion.LookRotation(forwardVektoru, gercekUpVektoru);

        if (citPrefab == null) return;

        GameObject yeniCit = Instantiate(citPrefab, citPozisyonu, citRotasyonu);
        NetworkObject netObj = yeniCit.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            aktifCitler.Add(yeniCit);
        }
        else
        {
            Destroy(yeniCit);
        }
    }

    public bool ArsaBizimMi(Vector2Int gridKoordinati)
    {
        return localSatinAlinanArsalar.Contains(gridKoordinati);
    }

    public Vector3 GridKoordinatiniPozisyonaCevir(Vector2Int gridKoordinati)
    {
        return new Vector3(gridKoordinati.x * parselBoyutu, 0, gridKoordinati.y * parselBoyutu);
    }
}