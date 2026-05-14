using UnityEngine;
using Unity.Netcode;

public class ChickenEggLayer : NetworkBehaviour
{
    [Header("Yumurta Ayarlarý")]
    public GameObject eggPrefab;

    [Header("Yumurtlama Alaný")]
    [Tooltip("Kümesteki BoxCollider'lý objeyi buraya sürükle.")]
    public BoxCollider eggSpawnArea;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DayNightCycleManager.YeniGunBasladiSinyali += LayEgg;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            DayNightCycleManager.YeniGunBasladiSinyali -= LayEgg;
        }
    }

    private void LayEgg()
    {
        if (eggPrefab == null || eggSpawnArea == null)
        {
            Debug.LogWarning("Tavuk yumurtlayacak ama yumurta prefabý veya alan seçili deðil!");
            return;
        }

        // --- YENÝ MANTIK: BELÝRLENEN ALANDA RASTGELE NOKTA SEÇME ---
        Bounds bounds = eggSpawnArea.bounds;
        float randomX = Random.Range(bounds.min.x, bounds.max.x);
        float randomZ = Random.Range(bounds.min.z, bounds.max.z);

        // Yumurtayý alanýn tam zemin seviyesine (merkez Y) býrakýyoruz
        Vector3 spawnPosition = new Vector3(randomX, bounds.center.y, randomZ);

        // Yumurtayý oluþtur
        GameObject spawnedEgg = Instantiate(eggPrefab, spawnPosition, Random.rotation);

        // Network üzerinden tüm oyunculara göster
        NetworkObject netObj = spawnedEgg.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }
    }
}