using Unity.Netcode;
using UnityEngine;

public class PlayerActionManager : NetworkBehaviour
{
    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
    }

    [ServerRpc]
    public void TohumEkServerRpc(int itemID, Vector3 nokta, int slotIndex)
    {
        // ItemData verisini çek (Resources içinden)
        ItemData data = Resources.Load<ItemData>("Items/" + itemID);

        // EkinPrefab'ın ItemData içine eklenmiş olması gerekir (Daha önce yaptığımız gibi)
        if (data != null && data.groundPrefab != null)
        {
            GameObject ekin = Instantiate(data.groundPrefab, nokta + Vector3.up * 0.05f, Quaternion.identity);
            ekin.GetComponent<NetworkObject>().Spawn();

            if (ekin.TryGetComponent(out ModularCrop sc))
            {
                sc.tohumID.Value = data.itemID;
            }

            // Envanterden düşür
            inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
        }
    }

    [ServerRpc]
    public void HasatEtServerRpc(ulong ekinNetID, Vector3 pos, bool urunVer, int ekstraUrun)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ekinNetID, out NetworkObject obj))
        {
            if (urunVer)
            {
                // TerrainLayerManager üzerinden tohum verisini bul
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

            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(pos, TerrainLayerManager.Instance.tilledLayerIndex, 3);
            }
        }
    }

    [ServerRpc]
    public void GubreleServerRpc(ulong cropNetId, float growthMultiplier, int yBonus, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(cropNetId, out NetworkObject cropObj))
        {
            if (cropObj.TryGetComponent(out ModularCrop targetCrop))
            {
                if (!targetCrop.isFertilized.Value)
                {
                    targetCrop.ApplyFertilizer(growthMultiplier, yBonus);
                    inventory.DecreaseItemAmountServerRpc(slotIndex, 1);
                }
            }
        }
    }

    [ServerRpc]
    public void RemoveTerrainDetailsServerRpc(Vector3 worldPos, float radius)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        TerrainData tData = terrain.terrainData;

        float prcEx = (worldPos.x - terrain.transform.position.x) / tData.size.x;
        float prcEz = (worldPos.z - terrain.transform.position.z) / tData.size.z;

        int posX = (int)(prcEx * tData.detailWidth);
        int posZ = (int)(prcEz * tData.detailHeight);
        int detRadius = Mathf.RoundToInt((radius / tData.size.x) * tData.detailWidth);

        int startX = Mathf.Clamp(posX - detRadius, 0, tData.detailWidth);
        int startZ = Mathf.Clamp(posZ - detRadius, 0, tData.detailHeight);
        int width = Mathf.Clamp(detRadius * 2, 0, tData.detailWidth - startX);
        int height = Mathf.Clamp(detRadius * 2, 0, tData.detailHeight - startZ);

        for (int i = 0; i < tData.detailPrototypes.Length; i++)
        {
            int[,] details = tData.GetDetailLayer(startX, startZ, width, height, i);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(detRadius, detRadius)) <= detRadius)
                    {
                        details[x, y] = 0;
                    }
                }
            }
            tData.SetDetailLayer(startX, startZ, i, details);
        }
    }
}