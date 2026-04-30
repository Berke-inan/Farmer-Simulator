using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public Terrain terrain;

    [Header("Yükseklik Ayarları")]
    public float heightScale = 50f; // Dağların maksimum yüksekliği
    public float noiseScale = 0.05f; // Gürültü sıklığı (Arttıkça daha çok dağ/çukur olur)
    [Range(0, 1)] public float baseHeight = 0.3f; // Arazi taban seviyesi (Örn: 0.3 deniz seviyesi gibi)
    public float seed = 0f; // Her seferinde farklı dünya için

    [Header("Detay (Ot) Ayarları")]
    [Range(0, 1)] public float detailThreshold = 0.5f; // Otların çıkma eşiği
    public float detailNoiseScale = 0.1f; // Otların kümelenme sıklığı

    [ContextMenu("Dünyayı Baştan Yarat")]
    public void GenerateWorld()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();

        // Her seferinde farklı bir sonuç için random seed
        seed = Random.Range(0f, 1000f);

        Debug.Log("Dünya oluşturuluyor...");
        GenerateHeights();
        GenerateDetails();

        terrain.Flush();
        Debug.Log("Dünya hazır!");
    }

    private void GenerateHeights()
    {
        TerrainData tData = terrain.terrainData;
        int res = tData.heightmapResolution;
        float[,] heights = new float[res, res];

        // Ölçeklendirmeyi terrain yüksekliğine göre önceden hesapla
        float adjustedScale = heightScale / tData.size.y;

        for (int x = 0; x < res; x++)
        {
            for (int z = 0; z < res; z++)
            {
                float xCoord = (float)x / res * noiseScale * 20f + seed;
                float zCoord = (float)z / res * noiseScale * 20f + seed;

                // Perlin Noise (0 ile 1 arası)
                float noise = Mathf.PerlinNoise(xCoord, zCoord);

                // KRİTİK NOKTA:
                // (noise - 0.5f) yaparak değeri -0.5 ile +0.5 arasına çekiyoruz.
                // Böylece hem yukarı hem aşağı hareket imkanı doğuyor.
                float relativeHeight = (noise - 0.5f) * adjustedScale;

                // baseHeight üzerine bu farkı ekliyoruz ve 0-1 arasına hapsediyoruz (clamp)
                heights[x, z] = Mathf.Clamp01(baseHeight + relativeHeight);
            }
        }
        tData.SetHeights(0, 0, heights);
    }

    private void GenerateDetails()
    {
        TerrainData tData = terrain.terrainData;
        int detRes = tData.detailResolution;
        int numLayers = tData.detailPrototypes.Length;

        if (numLayers == 0)
        {
            Debug.LogWarning("Terrain'e hiç detay (Detail Prototype) eklenmemiş!");
            return;
        }

        // --- YENİ EKLENEN KISIM: Zemin dokularını (Alphamaps) alıyoruz ---
        int alphaWidth = tData.alphamapWidth;
        int alphaHeight = tData.alphamapHeight;
        float[,,] alphas = tData.GetAlphamaps(0, 0, alphaWidth, alphaHeight);
        // ------------------------------------------------------------------

        int[][,] detailMaps = new int[numLayers][,];
        for (int l = 0; l < numLayers; l++)
        {
            detailMaps[l] = new int[detRes, detRes];
        }

        for (int z = 0; z < detRes; z++)
        {
            for (int x = 0; x < detRes; x++)
            {
                // --- YENİ EKLENEN KISIM: Ot koordinatını Doku koordinatına çevir ve kontrol et ---
                int alphaX = Mathf.RoundToInt(((float)x / detRes) * alphaWidth);
                int alphaZ = Mathf.RoundToInt(((float)z / detRes) * alphaHeight);

                alphaX = Mathf.Clamp(alphaX, 0, alphaWidth - 1);
                alphaZ = Mathf.Clamp(alphaZ, 0, alphaHeight - 1);

                // alphas array'i [Y, X, Layer] şeklinde çalışır (Y ekseni Z'yi temsil eder)
                float cimenAgirligi = alphas[alphaZ, alphaX, 0];

                // Eğer 0. layer (Çimen) ağırlığı 0.5'ten küçükse (yani başka bir şey boyalıysa)
                if (cimenAgirligi < 0.5f)
                {
                    // Ot koymayı atla ve döngüye devam et
                    continue;
                }
                // ---------------------------------------------------------------------------------

                // İhtimal filtresi kaldırıldı. Her noktaya KESİN detay konacak.
                int rastgeleKatman = Random.Range(0, numLayers);

                // Yoğunluk 16 (maksimum değer) olarak ayarlandı
                detailMaps[rastgeleKatman][z, x] = 16;
            }
        }

        for (int l = 0; l < numLayers; l++)
        {
            tData.SetDetailLayer(0, 0, l, detailMaps[l]);
        }

        terrain.Flush();
    }
}