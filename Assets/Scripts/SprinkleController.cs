using UnityEngine;
using Unity.Netcode;

public class SprinkleController : NetworkBehaviour
{
    [Header("Referanslar")]
    public TerrainCollider TerrainCollider;
    private TerrainLayerManager layerManager;

    [Header("Sulama Ayarları")]
    [Tooltip("Kaç saniyede bir sulama yapılacak?")]
    public float sulamaAraligi = 2f;
    [Tooltip("Boyama boyutu")]
    public int fircaBoyutu = 30;

    private float zamanlayici;

    void Start()
    {
        if (TerrainCollider != null)
        {
            layerManager = TerrainCollider.GetComponent<TerrainLayerManager>();
        }
        else
        {
            Debug.LogError("TerrainCollider referansı eksik!");
        }
    }

    void Update()
    {
        if (!IsServer) return;

        zamanlayici += Time.deltaTime;

        if (zamanlayici >= sulamaAraligi)
        {
            SulamaIslemi();
            zamanlayici = 0f;
        }
    }

    private void SulamaIslemi()
    {
        if (layerManager == null) return;

        Vector3 sulamaPozisyonu = transform.position;

        layerManager.PaintSoilServerRpc(sulamaPozisyonu, layerManager.wetLayerIndex, fircaBoyutu);
    }
}
