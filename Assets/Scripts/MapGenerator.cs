using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject grassPrefab;
    public GameObject soilPrefab;

    [Header("Harita Boyutları")]
    public int mapWidth = 700; // Test ederken düşük tutmayı unutma
    public int mapLength = 700;
    public float tileSize = 1f;

    [Header("Toprak Alanı Ayarları")]
    public int soilWidth = 10;
    public int soilLength = 10;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        // Toprak alanın index sınırlarını belirliyoruz
        int soilStartX = (mapWidth - soilWidth) / 2;
        int soilStartZ = (mapLength - soilLength) / 2;

        int soilEndX = soilStartX + soilWidth;
        int soilEndZ = soilStartZ + soilLength;

        // Haritanın (0,0,0) merkezine oturması için gereken kaydırma miktarı
        float offsetX = (mapWidth - 1) / 2f;
        float offsetZ = (mapLength - 1) / 2f;

        Transform parentTransform = this.transform;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapLength; z++)
            {
                // Mevcut index'ten offset'i çıkararak gerçek dünya pozisyonunu buluyoruz
                float posX = (x - offsetX) * tileSize;
                float posZ = (z - offsetZ) * tileSize;

                Vector3 spawnPosition = new Vector3(posX, 0, posZ);

                GameObject prefabToSpawn = grassPrefab;

                // Indexler toprak alanına denk geliyorsa prefabı değiştir
                if (x >= soilStartX && x < soilEndX && z >= soilStartZ && z < soilEndZ)
                {
                    prefabToSpawn = soilPrefab;
                }

                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, parentTransform);
            }
        }
    }
}