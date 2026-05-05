using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TimedTerrainChange
{
    public Vector3 worldPos;
    public float expirationTime;
    public int targetLayer;
    public int brushSize;
}

[System.Serializable]
public class TohumVerisi
{
    public string tohumAdi;
    public int tohumID;
    public float asamaGecisSuresi = 10f;
    public GameObject dusecekTohumPrefab;
    public int hasatMiktari = 3;
}

public class TerrainLayerManager : NetworkBehaviour
{
    public static TerrainLayerManager Instance;
    public Terrain terrain;

    [Header("Ekin Veritabanı")]
    public List<TohumVerisi> tohumListesi = new List<TohumVerisi>();

    [Header("Layer Ayarları")]
    public int normalLayerIndex = 0;
    public int tilledLayerIndex = 1;
    public int wetLayerIndex = 2;
    public int defaultBrushSize = 3;

    [Header("Zaman Ayarları")]
    public float kurumaSuresi = 60f;
    public float duzelmeSuresi = 120f;

    private List<TimedTerrainChange> activeChanges = new List<TimedTerrainChange>();

    private void Awake() => Instance = this;

    void Update()
    {
        if (!IsServer || activeChanges.Count == 0) return;

        for (int i = activeChanges.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeChanges[i].expirationTime)
            {
                if (activeChanges[i].targetLayer == normalLayerIndex)
                {
                    Collider[] ekinler = Physics.OverlapSphere(activeChanges[i].worldPos, activeChanges[i].brushSize * 0.5f);
                    bool ekinVarMi = false;

                    foreach (var col in ekinler)
                    {
                        if (col.GetComponentInParent<ModularCrop>() != null)
                        {
                            ekinVarMi = true;
                            break;
                        }
                    }

                    if (ekinVarMi)
                    {
                        activeChanges.RemoveAt(i);
                        continue;
                    }
                }

                // DÜZELTME BURADA: Zamanlayıcılar kendi güvenli fonksiyonunu çağırır. Normal çapa kuralını kullanmaz.
                ZamanlayiciBoyamaClientRpc(activeChanges[i].worldPos, activeChanges[i].targetLayer, activeChanges[i].brushSize);

                if (activeChanges[i].targetLayer == tilledLayerIndex)
                {
                    activeChanges.Add(new TimedTerrainChange
                    {
                        worldPos = activeChanges[i].worldPos,
                        expirationTime = Time.time + duzelmeSuresi,
                        targetLayer = normalLayerIndex,
                        brushSize = activeChanges[i].brushSize
                    });
                }

                activeChanges.RemoveAt(i);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PaintSoilServerRpc(Vector3 worldPos, int layerIndex, int brushSize)
    {
       
        PaintSoilClientRpc(worldPos, layerIndex, brushSize);

        if (IsServer)
        {
            TerrainData tData = terrain.terrainData;
            float gercekDunyaYaricapi = (brushSize * (tData.size.x / tData.alphamapWidth)) * 0.5f;

            for (int i = activeChanges.Count - 1; i >= 0; i--)
            {
                if (Vector3.Distance(activeChanges[i].worldPos, worldPos) <= gercekDunyaYaricapi)
                {
                    activeChanges.RemoveAt(i);
                }
            }

            float duration = (layerIndex == wetLayerIndex) ? kurumaSuresi : duzelmeSuresi;
            int nextLayer = (layerIndex == wetLayerIndex) ? tilledLayerIndex : normalLayerIndex;

            activeChanges.Add(new TimedTerrainChange
            {
                worldPos = worldPos,
                expirationTime = Time.time + duration,
                targetLayer = nextLayer,
                brushSize = brushSize
            });
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PaintSoilClientRpc(Vector3 worldPos, int layerIndex, int brushSize)
    {
        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;

        int mapX = (int)((terrainPos.x / tData.size.x) * tData.alphamapWidth);
        int mapZ = (int)((terrainPos.z / tData.size.z) * tData.alphamapHeight);

        int offset = brushSize / 2;
        mapX = Mathf.Clamp(mapX, offset, tData.alphamapWidth - offset);
        mapZ = Mathf.Clamp(mapZ, offset, tData.alphamapHeight - offset);

        float[,,] alphas = tData.GetAlphamaps(mapX - offset, mapZ - offset, brushSize, brushSize);
        float center = brushSize / 2f;

        for (int i = 0; i < brushSize; i++)
        {
            for (int j = 0; j < brushSize; j++)
            {
                if (Vector2.Distance(new Vector2(i, j), new Vector2(center, center)) <= center)
                {
                    bool boyanabilir = true;

                    if (layerIndex == wetLayerIndex)
                    {
                        if (alphas[i, j, tilledLayerIndex] < 0.5f && alphas[i, j, wetLayerIndex] < 0.5f)
                        {
                            boyanabilir = false;
                        }
                    }
                    else if (layerIndex == tilledLayerIndex)
                    {
                        if (alphas[i, j, normalLayerIndex] < 0.5f && alphas[i, j, wetLayerIndex] < 0.5f)
                        {
                            boyanabilir = false;
                        }
                    }

                    if (boyanabilir)
                    {
                        for (int l = 0; l < tData.terrainLayers.Length; l++)
                        {
                            alphas[i, j, l] = (l == layerIndex) ? 1f : 0f;
                        }
                    }
                }
            }
        }
        tData.SetAlphamaps(mapX - offset, mapZ - offset, alphas);

        if (layerIndex == tilledLayerIndex)
        {
            int detRes = tData.detailResolution;
            int numDetailLayers = tData.detailPrototypes.Length;

            int detX = (int)((terrainPos.x / tData.size.x) * detRes);
            int detZ = (int)((terrainPos.z / tData.size.z) * detRes);

            float ratio = (float)detRes / tData.alphamapWidth;
            int dBrush = Mathf.Max(1, Mathf.RoundToInt(brushSize * ratio));
            int dOffset = dBrush / 2;

            int startX = Mathf.Clamp(detX - dOffset, 0, detRes - 1);
            int startZ = Mathf.Clamp(detZ - dOffset, 0, detRes - 1);
            int endX = Mathf.Clamp(detX + dOffset, 0, detRes - 1);
            int endZ = Mathf.Clamp(detZ + dOffset, 0, detRes - 1);

            int sizeX = endX - startX;
            int sizeZ = endZ - startZ;

            if (sizeX > 0 && sizeZ > 0 && numDetailLayers > 0)
            {
                for (int l = 0; l < numDetailLayers; l++)
                {
                    int[,] details = tData.GetDetailLayer(startX, startZ, sizeX, sizeZ, l);
                    for (int z = 0; z < sizeZ; z++)
                    {
                        for (int x = 0; x < sizeX; x++) details[z, x] = 0;
                    }
                    tData.SetDetailLayer(startX, startZ, l, details);
                }
            }
        }
    }

    // YENİ FONKSİYON: Sadece zamanlayıcıların arka planda kullandığı güvenli boyama işlemi. Çimenlere dokunmaz.
    [Rpc(SendTo.ClientsAndHost)]
    private void ZamanlayiciBoyamaClientRpc(Vector3 worldPos, int targetLayer, int brushSize)
    {
        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;

        int mapX = (int)((terrainPos.x / tData.size.x) * tData.alphamapWidth);
        int mapZ = (int)((terrainPos.z / tData.size.z) * tData.alphamapHeight);

        int offset = brushSize / 2;
        mapX = Mathf.Clamp(mapX, offset, tData.alphamapWidth - offset);
        mapZ = Mathf.Clamp(mapZ, offset, tData.alphamapHeight - offset);

        float[,,] alphas = tData.GetAlphamaps(mapX - offset, mapZ - offset, brushSize, brushSize);
        float center = brushSize / 2f;

        for (int i = 0; i < brushSize; i++)
        {
            for (int j = 0; j < brushSize; j++)
            {
                if (Vector2.Distance(new Vector2(i, j), new Vector2(center, center)) <= center)
                {
                    bool degistir = false;

                    if (targetLayer == tilledLayerIndex)
                    {
                        // KURUMA İŞLEMİ: Sadece ISLAK olan yeri kuru yap. Asla çimeni çapalama.
                        if (alphas[i, j, wetLayerIndex] >= 0.5f) degistir = true;
                    }
                    else if (targetLayer == normalLayerIndex)
                    {
                        // NORMALE DÖNME: Sadece KURU olan yeri çimen yap.
                        if (alphas[i, j, tilledLayerIndex] >= 0.5f) degistir = true;
                    }

                    if (degistir)
                    {
                        for (int l = 0; l < tData.terrainLayers.Length; l++)
                            alphas[i, j, l] = (l == targetLayer) ? 1f : 0f;
                    }
                }
            }
        }
        tData.SetAlphamaps(mapX - offset, mapZ - offset, alphas);
    }

    public bool IsLayerDominant(Vector3 worldPos, int targetLayerIndex)
    {
        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;
        int mapX = (int)((terrainPos.x / tData.size.x) * tData.alphamapWidth);
        int mapZ = (int)((terrainPos.z / tData.size.z) * tData.alphamapHeight);

        mapX = Mathf.Clamp(mapX, 0, tData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, tData.alphamapHeight - 1);

        float[,,] alpha = tData.GetAlphamaps(mapX, mapZ, 1, 1);

        return alpha[0, 0, targetLayerIndex] > 0.5f;
    }

    public bool IsSoilTilled(Vector3 worldPos)
    {
        return IsLayerDominant(worldPos, tilledLayerIndex) || IsLayerDominant(worldPos, wetLayerIndex);
    }

    public bool IsSoilWet(Vector3 worldPos)
    {
        return IsLayerDominant(worldPos, wetLayerIndex);
    }

    public TohumVerisi GetTohumVerisi(int id) => tohumListesi.Find(t => t.tohumID == id);
}