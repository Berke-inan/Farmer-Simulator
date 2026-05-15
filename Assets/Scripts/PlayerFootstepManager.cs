using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepManager : NetworkBehaviour
{
    [Header("Mesafe ve Hýz Ayarlarý")]
    public float walkStepDistance = 3.5f; // Yürüme ritmi için bu mesafeyi ARTIR (Örn: 3.5 - 4.5)
    public float runStepDistance = 5.5f;  // Koþma ritmi için bu mesafe (Örn: 5.5 - 7.0)

    [Tooltip("Karakterin hýzý bu deðeri geçerse KOÞUYOR sayýlýr. Console'daki hýza bakarak ayarla!")]
    public float runSpeedThreshold = 6.0f;

    [Header("Zemin Sesleri")]
    public AudioClip dirtFootstepClip;
    public AudioClip rockFootstepClip;
    public AudioClip woodFootstepClip;

    [Header("Ses Yükseklikleri")]
    [Range(0f, 1f)] public float dirtVolume = 0.4f;
    [Range(0f, 2f)] public float rockVolume = 1.2f; // Kýsýk sesler için çarpan
    [Range(0f, 1f)] public float woodVolume = 0.4f;
    public float runVolumeMultiplier = 1.4f;

    [Header("Zamanlama")]
    public float minTimeBetweenSteps = 0.35f;

    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceTraveled;
    private float lastStepTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    private void Start() => lastPosition = transform.position;

    private void Update()
    {
        if (!IsOwner) return;

        float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
        distanceTraveled += distanceThisFrame;
        lastPosition = transform.position;

        // Gerçek hýz hesaplama (Metre/Saniye)
        float currentSpeed = distanceThisFrame / Time.deltaTime;

        // Hýz 0.1'den küçükse karakter duruyordur, iþlem yapma
        if (currentSpeed < 0.1f)
        {
            distanceTraveled = 0f;
            return;
        }

        bool isRunning = currentSpeed > runSpeedThreshold;
        float currentStepDistance = isRunning ? runStepDistance : walkStepDistance;

        // --- DEBUG LOG: RÝTMÝ AYARLAMAK ÝÇÝN BURAYA BAK ---
        // Console'da hýzýný ve hangi modda olduðunu göreceksin
        if (distanceTraveled > 0.1f)
        {
            // Debug.Log($"Hýz: {currentSpeed:F1} | Mod: {(isRunning ? "KOÞU" : "YÜRÜME")} | Mesafe: {distanceTraveled:F1}/{currentStepDistance}");
        }

        if (distanceTraveled >= currentStepDistance && Time.time - lastStepTime >= minTimeBetweenSteps)
        {
            distanceTraveled = 0f;
            lastStepTime = Time.time;
            PlayFootstepSound(isRunning);
        }
    }

    private void PlayFootstepSound(bool isRunning)
    {
        AudioClip clipToPlay = woodFootstepClip;
        float baseVolume = woodVolume;

        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 1.5f))
        {
            if (hit.collider.TryGetComponent(out Terrain terrain))
            {
                int texIndex = GetDominantTerrainTexture(hit.point, terrain);
                if (texIndex == 3) { clipToPlay = rockFootstepClip; baseVolume = rockVolume; }
                else { clipToPlay = dirtFootstepClip; baseVolume = dirtVolume; }
            }
        }

        if (clipToPlay != null)
        {
            float finalVolume = isRunning ? (baseVolume * runVolumeMultiplier) : baseVolume;
            audioSource.pitch = Random.Range(isRunning ? 1.1f : 0.95f, isRunning ? 1.25f : 1.05f);
            audioSource.PlayOneShot(clipToPlay, finalVolume);
        }
    }

    private int GetDominantTerrainTexture(Vector3 worldPosition, Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = terrain.transform.position;
        float mapX = ((worldPosition.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth;
        float mapZ = ((worldPosition.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight;
        int x = Mathf.Clamp(Mathf.FloorToInt(mapX), 0, terrainData.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(mapZ), 0, terrainData.alphamapHeight - 1);
        float[,,] alphamap = terrainData.GetAlphamaps(x, z, 1, 1);
        float maxWeight = 0f; int maxIndex = 0;
        for (int i = 0; i < terrainData.alphamapLayers; i++)
        {
            if (alphamap[0, 0, i] > maxWeight) { maxWeight = alphamap[0, 0, i]; maxIndex = i; }
        }
        return maxIndex;
    }
}