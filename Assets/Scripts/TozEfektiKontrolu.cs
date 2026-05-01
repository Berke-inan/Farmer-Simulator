using UnityEngine;

public class TozEfektiKontrolu : MonoBehaviour
{
    [Header("Baðlantýlar")]
    [Tooltip("Pulluðun ana gövdesindeki AttachableEquipment kodunu buraya sürükle")]
    public AttachableEquipment anaGovde;

    [Header("Görsel Efektler")]
    [Tooltip("Çalýþýrken çýkmasýný istediðin toz efektlerini (Particle System) buraya sürükle")]
    public ParticleSystem[] tozEfektleri;

    [Header("Zemin Ayarlarý")]
    [Tooltip("Taþ zeminin Terrain Layers içindeki sýrasý (0'dan baþlar). Fotoðraftaki taþ 4. sýrada olduðu için indexi 3'tür.")]
    public int tasZeminIndex = 3;

    private void Awake()
    {
        // Eðer ana gövdeyi elinle atamayý unutursan, kod otomatik olarak bulmaya çalýþsýn
        if (anaGovde == null)
        {
            anaGovde = GetComponentInParent<AttachableEquipment>();
        }
    }

    private void Update()
    {
        // Ana gövde yoksa hiçbir þey yapma
        if (anaGovde == null) return;

        bool makineCalisiyorMu = anaGovde.isWorking.Value;
        bool tozKalkmaliMi = false; // Baþlangýçta toz kalkmasýn diyoruz

        // SADECE makine çalýþýyorsa zemin kontrolü yap (Boþ yere performansý yormayalým)
        if (makineCalisiyorMu)
        {
            // Pulluðun merkezinden aþaðý doðru 5 metrelik bir lazer yolla
            if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 5f))
            {
                Terrain terrain = hit.collider.GetComponent<Terrain>();

                // Eðer lazer Terrain'e (Topraða) çarptýysa
                if (terrain != null)
                {
                    // Lazerin deðdiði noktadaki en yoðun dokuyu hesapla
                    int baskinDoku = BaskinDokuyuBul(hit.point, terrain);

                    // Eðer o anki doku, bizim TAÞ ZEMÝN (3) DEÐÝLSE, toz kalkabilir!
                    if (baskinDoku != tasZeminIndex)
                    {
                        tozKalkmaliMi = true;
                    }
                }
            }
        }

        // Bütün toz efektlerini kontrol et
        foreach (ParticleSystem toz in tozEfektleri)
        {
            if (toz != null)
            {
                // Toz kalkmasý gerekiyorsa ve þu an kapalýysa -> BAÞLAT
                if (tozKalkmaliMi && !toz.isPlaying)
                {
                    toz.Play();
                }
                // Toz kalkmamasý gerekiyorsa (makine durduysa veya taþa çýktýysa) -> DURDUR
                else if (!tozKalkmaliMi && toz.isPlaying)
                {
                    toz.Stop();
                }
            }
        }
    }

    // --- UNITY TERRAIN DOKU OKUMA MATEMATÝÐÝ ---
    private int BaskinDokuyuBul(Vector3 dunyaPozisyonu, Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPozisyonu = terrain.transform.position;

        // Dünya pozisyonunu Terrain'in "Alphamap" (Doku Haritasý) koordinatlarýna çeviriyoruz
        int mapX = Mathf.RoundToInt(((dunyaPozisyonu.x - terrainPozisyonu.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((dunyaPozisyonu.z - terrainPozisyonu.z) / terrainData.size.z) * terrainData.alphamapHeight);

        // Harita dýþýna çýkýldýysa -1 döndür
        if (mapX < 0 || mapZ < 0 || mapX >= terrainData.alphamapWidth || mapZ >= terrainData.alphamapHeight)
            return -1;

        // O tam noktadaki bütün dokularýn karýþým oranlarýný (Aðýrlýklarýný) al
        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        int enBaskinIndex = 0;
        float enYuksekOran = 0f;

        // Hangi dokunun oraný daha fazlaysa onu buluyoruz
        for (int i = 0; i < terrainData.alphamapLayers; i++)
        {
            if (splatmapData[0, 0, i] > enYuksekOran)
            {
                enYuksekOran = splatmapData[0, 0, i];
                enBaskinIndex = i;
            }
        }

        return enBaskinIndex;
    }
}