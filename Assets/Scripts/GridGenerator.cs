using Unity.Netcode;
using UnityEngine;

// Ağ işlemleri yapabilmesi için MonoBehaviour yerine NetworkBehaviour'dan miras alınmalıdır
public class GridGenerator : NetworkBehaviour
{
    [Header("Grid Ayarları")]
    public GameObject tilePrefab;
    public int columns = 4;
    public int rows = 4;

    [Header("Ölçüler (Scale Değerleri)")]
    public float spacingX = 5f;
    public float spacingZ = 5f;

    [Header("Başlangıç Pozisyonu")]
    public Vector3 startPosition = new Vector3(2.5f, -0.4f, 52.47f);

    // Start yerine OnNetworkSpawn kullanılır. Bu sayede sadece ağ tamamen hazır olduğunda çalışır.
    public override void OnNetworkSpawn()
    {
        // Harita üretimini sadece Server (veya Host) yapmalıdır
        if (IsServer)
        {
            GenerateGrid();
        }
    }

    private void GenerateGrid()
    {
        if (tilePrefab == null) return;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                float posX = startPosition.x + (x * spacingX);
                float posZ = startPosition.z + (z * spacingZ);
                Vector3 spawnPos = new Vector3(posX, startPosition.y, posZ);

                GameObject newTile = Instantiate(tilePrefab, spawnPos, Quaternion.identity);
                NetworkObject netObj = newTile.GetComponent<NetworkObject>();

                // 1. Obveyi ağ üzerindeki herkes için görünür hale getir
                netObj.Spawn();

                // 2. Normal "transform.parent" kullanmak yerine Netcode'un güvenli ebeveyn atama yöntemi kullanılır
                netObj.TrySetParent(this.transform);

                newTile.name = $"Zemin_{x}_{z}";
            }
        }
    }
}