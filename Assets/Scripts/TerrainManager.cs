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

        // ÖNEMLİ: Mevcut tüm detayları temizleyelim (çalı, çimen ne varsa)
        for (int l = 0; l < tData.detailPrototypes.Length; l++)
        {
            tData.SetDetailLayer(0, 0, l, new int[detRes, detRes]);
        }

        // Haritayı hazırla
        int[,] detailMap = new int[detRes, detRes];

        for (int z = 0; z < detRes; z++)
        {
            for (int x = 0; x < detRes; x++)
            {
                // Değeri 16 yaparsan o kareyi tamamen doldurur (en yoğun hali)
                // Eğer çalılar çok iç içe girerse bu sayıyı 5-10 arasına çekersin.
                detailMap[z, x] = 16;
            }
        }

        // Hangi katmana basıyoruz? 
        // Eğer sadece bir tane çalı eklediysen o 0. indextedir.
        tData.SetDetailLayer(0, 0, 0, detailMap);

        terrain.Flush();
    }
}