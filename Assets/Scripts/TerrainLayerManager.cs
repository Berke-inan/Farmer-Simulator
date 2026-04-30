using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct TimedTerrainChange
{
    public Vector3 worldPos;
    public float expirationTime;
    public int targetLayer; // Dönüşeceği layer
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
    public int brushSize = 3;

    [Header("Zaman Ayarları")]
    public float kurumaSuresi = 60f; // Islak -> Çapalanmış
    public float duzelmeSuresi = 120f; // Çapalanmış -> Normal

    private List<TimedTerrainChange> activeChanges = new List<TimedTerrainChange>();

    private void Awake() => Instance = this;

    void Update()
    {
        if (!IsServer || activeChanges.Count == 0) return;

        for (int i = activeChanges.Count - 1; i >= 0; i--)
        {
            if (Time.time >= activeChanges[i].expirationTime)
            {
                // Kritik Kontrol: Eğer hedef Normal Layer (Çimen) ise
                if (activeChanges[i].targetLayer == normalLayerIndex)
                {
                    Collider[] ekinler = Physics.OverlapSphere(activeChanges[i].worldPos, 3f);
                    bool ekinVarMi = false;

                    foreach (var col in ekinler)
                    {
                        if (col.GetComponentInParent<ModularCrop>() != null)
                        {
                            ekinVarMi = true;
                            break;
                        }
                    }

                    // Eğer bitki bulunursa, toprağı çimene çevirmeyi iptal et
                    if (ekinVarMi)
                    {
                        activeChanges.RemoveAt(i);
                        continue;
                    }
                }

                // Normal kuruma veya düzelme işlemini yap
                PaintSoilClientRpc(activeChanges[i].worldPos, activeChanges[i].targetLayer);

                if (activeChanges[i].targetLayer == tilledLayerIndex)
                {
                    activeChanges.Add(new TimedTerrainChange
                    {
                        worldPos = activeChanges[i].worldPos,
                        expirationTime = Time.time + duzelmeSuresi,
                        targetLayer = normalLayerIndex
                    });
                }

                activeChanges.RemoveAt(i);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PaintSoilServerRpc(Vector3 worldPos, int layerIndex)
    {
        // 1. KURAL: Çapalama işlemi sadece zemin 0. layer (Normal/Çimen) ise yapılabilir.
        if (layerIndex == tilledLayerIndex)
        {
            if (!IsLayerDominant(worldPos, normalLayerIndex)) return;
        }
        // 2. KURAL: Sulama işlemi sadece zemin 1. layer (Tilled/Çapalanmış) ise yapılabilir.
        else if (layerIndex == wetLayerIndex)
        {
            if (!IsLayerDominant(worldPos, tilledLayerIndex)) return;
        }

        PaintSoilClientRpc(worldPos, layerIndex);

        // Zamanlayıcıya ekle
        if (IsServer)
        {
            float duration = (layerIndex == wetLayerIndex) ? kurumaSuresi : duzelmeSuresi;
            int nextLayer = (layerIndex == wetLayerIndex) ? tilledLayerIndex : normalLayerIndex;

            activeChanges.Add(new TimedTerrainChange
            {
                worldPos = worldPos,
                expirationTime = Time.time + duration,
                targetLayer = nextLayer
            });
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PaintSoilClientRpc(Vector3 worldPos, int layerIndex)
    {
        TerrainData tData = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;

        // --- 1. KISIM: ZEMİN DOKUSUNU BOYAMA ---
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
                    for (int l = 0; l < tData.terrainLayers.Length; l++)
                        alphas[i, j, l] = (l == layerIndex) ? 1f : 0f;
                }
            }
        }
        tData.SetAlphamaps(mapX - offset, mapZ - offset, alphas);

        // --- 2. KISIM: OTLARI VE ÇALILARI SİLME ---
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
                        for (int x = 0; x < sizeX; x++)
                        {
                            details[z, x] = 0;
                        }
                    }
                    tData.SetDetailLayer(startX, startZ, l, details);
                }
            }
        }
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