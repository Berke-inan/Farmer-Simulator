using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class TractorFXController : NetworkBehaviour
{
    private TractorController tractorController;
    private TractorFuelSystem fuelSystem; // YENÝ: Motor beyni referansý
    private Rigidbody rb;

    [Header("Partikül Sistemleri")]
    [Tooltip("Traktörün egzoz dumaný objesi")]
    public ParticleSystem exhaustSmoke;
    [Tooltip("Arka tekerleklerin çamur fýrlatma objeleri")]
    public ParticleSystem[] wheelMuds;

    [Header("Duman Ayarlarý")]
    public float idleSmokeRate = 15f;    // Rölantide (Dururken) çýkan duman
    public float maxSmokeRate = 70f;     // Tam gaz giderken çýkan duman

    [Header("Çamur Ayarlarý")]
    public float minSpeedForMud = 2.5f;  // Çamurun fýrlamaya baþlayacaðý minimum hýz
    public float maxMudEmission = 80f;   // En yüksek hýzdaki çamur miktarý
    public float maxSpeed = 15f;         // Aracýn tahmini son hýzý

    [Header("Zemin Ayarlarý")]
    [Tooltip("Taþ zeminin Terrain Layers içindeki sýrasý (0'dan baþlar).")]
    public int tasZeminIndex = 3;

    [Header("Çamur Dinamik Görünüm")]
    [Tooltip("Hýza göre çamurun saydamlýðý (Alpha) 30 ile 160 arasýnda deðiþir.")]
    public float minMudAlpha = 30f;
    public float maxMudAlpha = 160f;
    [Tooltip("Hýza göre çamurun fýrlama hýzý (Start Speed) 10 ile 20 arasýnda deðiþir.")]
    public float minMudStartSpeed = 10f;
    public float maxMudStartSpeed = 20f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();
        fuelSystem = GetComponent<TractorFuelSystem>(); // YENÝ: Yakýt sistemini bul

        if (exhaustSmoke == null)
            Debug.LogWarning($"{gameObject.name} üzerinde 'Exhaust Smoke' partikülü eksik!");
    }

    private void Update()
    {
        // YENÝ MANTIK: Sadece biri bindiðinde deðil, MOTOR ÇALIÞIYORSA efektleri oynat
        bool isEngineOn = fuelSystem != null && fuelSystem.isEngineRunning.Value;

        if (!isEngineOn)
        {
            if (exhaustSmoke != null && exhaustSmoke.isPlaying) StopAllEffects();
            return; // Motor kapalýysa aþaðý inme (Duman ve çamur yok)
        }

        float currentSpeed = rb.linearVelocity.magnitude;

        // --- EGZOZ DUMANI YÖNETÝMÝ ---
        if (exhaustSmoke != null)
        {
            if (!exhaustSmoke.isPlaying) exhaustSmoke.Play();

            var emission = exhaustSmoke.emission;
            if (!emission.enabled) emission.enabled = true;

            float speedFactor = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
            float targetRate = Mathf.Lerp(idleSmokeRate, maxSmokeRate, speedFactor);

            emission.rateOverTime = targetRate;
        }

        // --- ÇAMUR YÖNETÝMÝ ---
        ManageMud(currentSpeed);
    }

    private void ManageMud(float speed)
    {
        float mudRate = 0f;
        float speedFactor = 0f;
        bool tasZemindeMi = false;

        // 1. ZEMÝN KONTROLÜ
        if (Physics.Raycast(transform.position + Vector3.up * 1f, Vector3.down, out RaycastHit hit, 5f))
        {
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                int baskinDoku = BaskinDokuyuBul(hit.point, terrain);
                if (baskinDoku == tasZeminIndex)
                {
                    tasZemindeMi = true;
                }
            }
        }

        // 2. HIZ VE ÇAMUR ÜRETÝM HESAPLAMASI
        if (speed >= minSpeedForMud && !tasZemindeMi)
        {
            speedFactor = Mathf.InverseLerp(minSpeedForMud, maxSpeed, speed);
            mudRate = Mathf.Lerp(0, maxMudEmission, speedFactor);
        }

        // 3. DÝNAMÝK GÖRÜNÜM HESAPLAMALARI
        float currentAlpha = Mathf.Lerp(minMudAlpha, maxMudAlpha, speedFactor) / 255f;
        float currentStartSpeed = Mathf.Lerp(minMudStartSpeed, maxMudStartSpeed, speedFactor);

        foreach (var mud in wheelMuds)
        {
            if (mud != null)
            {
                var emission = mud.emission;
                var main = mud.main;

                if (mudRate > 0 && !mud.isPlaying) mud.Play();

                emission.rateOverTime = mudRate;
                main.startSpeed = currentStartSpeed;

                Color tempColor = main.startColor.color;
                tempColor.a = currentAlpha;
                main.startColor = tempColor;

                if (mudRate <= 0 && mud.isPlaying) mud.Stop();
            }
        }
    }

    private int BaskinDokuyuBul(Vector3 dunyaPozisyonu, Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPozisyonu = terrain.transform.position;

        int mapX = Mathf.RoundToInt(((dunyaPozisyonu.x - terrainPozisyonu.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = Mathf.RoundToInt(((dunyaPozisyonu.z - terrainPozisyonu.z) / terrainData.size.z) * terrainData.alphamapHeight);

        if (mapX < 0 || mapZ < 0 || mapX >= terrainData.alphamapWidth || mapZ >= terrainData.alphamapHeight)
            return -1;

        float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        int enBaskinIndex = 0;
        float enYuksekOran = 0f;

        for (int i = 0; i < terrainData.alphamapLayers; i++)
        {
            if (splatmapData[0, 0, i] > enYuksekOran)
            {
                enYuksekOran = splatmapData[0, 0, i];
                enBaskinIndex = i;
            }
        }

        return enBaskinIndex;
    }

    private void StopAllEffects()
    {
        if (exhaustSmoke != null) exhaustSmoke.Stop();

        foreach (var mud in wheelMuds)
        {
            if (mud != null) mud.Stop();
        }
    }
}