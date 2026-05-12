using Unity.Netcode;
using UnityEngine;

public class PlayerActionManager : NetworkBehaviour
{
    private InventoryManager inventory;

    private void Awake()
    {
        inventory = GetComponent<InventoryManager>();
    }

    // --- TOHUM EKME ---
    [ServerRpc]
    public void TohumEkServerRpc(string itemID, Vector3 nokta, int slotIndex)
    {
        ItemData data = ItemDatabase.Instance.GetItemByID(itemID);
        if (data != null && data.EkinPrefab != null)
        {
            GameObject ekin = Instantiate(data.EkinPrefab, nokta + Vector3.up * 0.05f, Quaternion.identity);
            ekin.GetComponent<NetworkObject>().Spawn();

            if (ekin.TryGetComponent(out ModularCrop sc))
            {
                sc.tohumID.Value = data.TohumID;
            }
            inventory.RemoveItemServerRpc(slotIndex, 1, nokta, Vector3.up, false);
        }
    }

    // --- HASAT ETME ---
    [ServerRpc]
    public void HasatEtServerRpc(ulong ekinNetID, Vector3 pos, bool urunVer, int ekstraUrun)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ekinNetID, out NetworkObject obj))
        {
            if (urunVer)
            {
                TohumVerisi v = TerrainLayerManager.Instance.tohumListesi.Find(x => obj.name.Contains(x.tohumAdi));
                if (v != null)
                {
                    int toplamUrun = v.hasatMiktari + ekstraUrun;
                    for (int i = 0; i < toplamUrun; i++)
                    {
                        Vector3 off = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                        GameObject t = Instantiate(v.dusecekTohumPrefab, pos + off, Quaternion.identity);
                        t.GetComponent<NetworkObject>().Spawn();
                    }
                }
            }

            bool wasWet = TerrainLayerManager.Instance.IsSoilWet(pos);
            obj.Despawn();
            Destroy(obj.gameObject);

            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(pos, TerrainLayerManager.Instance.tilledLayerIndex, 3);
            }
        }
    }

    // --- GÜBRELEME ---
    // PlayerActionManager.cs içine eklenecek/güncellenecek kısım
    [ServerRpc]
    public void GubreleServerRpc(ulong cropNetId, float growthMultiplier, int yBonus, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropNetId, out NetworkObject cropObj))
        {
            if (cropObj.TryGetComponent(out ModularCrop targetCrop))
            {
                // Ekin zaten gübrelenmiş mi kontrol et
                if (!targetCrop.isFertilized.Value)
                {
                    // Gübreleme mantığını çalıştır
                    targetCrop.ApplyFertilizer(growthMultiplier, yBonus);

                    // Envanterden 1 adet eksilt (kendi local değişkenimiz olan inventory üzerinden)
                    inventory.RemoveItemServerRpc(slotIndex, 1, transform.position, Vector3.up, false);
                }
            }
        }
    }


    // PlayerActionManager.cs içine eklenecek yeni metod
    [ServerRpc]
    public void RemoveTerrainDetailsServerRpc(Vector3 worldPos, float radius)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        TerrainData tData = terrain.terrainData;

        // 1. Dünya koordinatlarını Detail Map (0-512 veya 0-1024) koordinatlarına çevir
        float prcEx = (worldPos.x - terrain.transform.position.x) / tData.size.x;
        float prcEz = (worldPos.z - terrain.transform.position.z) / tData.size.z;

        int posX = (int)(prcEx * tData.detailWidth);
        int posZ = (int)(prcEz * tData.detailHeight);

        // Yarıçapı detail map ölçeğine çevir
        int detRadius = Mathf.RoundToInt((radius / tData.size.x) * tData.detailWidth);

        // 2. Belirlenen alanı tara ve temizle
        int startX = Mathf.Clamp(posX - detRadius, 0, tData.detailWidth);
        int startZ = Mathf.Clamp(posZ - detRadius, 0, tData.detailHeight);
        int width = Mathf.Clamp(detRadius * 2, 0, tData.detailWidth - startX);
        int height = Mathf.Clamp(detRadius * 2, 0, tData.detailHeight - startZ);

        // Terrain üzerindeki tüm detay katmanlarını (ot türlerini) tek tek temizle
        for (int i = 0; i < tData.detailPrototypes.Length; i++)
        {
            int[,] details = tData.GetDetailLayer(startX, startZ, width, height, i);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Daire şeklinde silme yapmak için mesafe kontrolü (opsiyonel ama daha güzel durur)
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(detRadius, detRadius)) <= detRadius)
                    {
                        details[x, y] = 0;
                    }
                }
            }
            // Değişikliği sunucu tarafında terrain verisine uygula
            tData.SetDetailLayer(startX, startZ, i, details);
        }
    }
}