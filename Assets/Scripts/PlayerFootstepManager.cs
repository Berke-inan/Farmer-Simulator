using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepManager : NetworkBehaviour
{
    [Header("Mesafe ve Hýz Ayarlarý")]
    public float walkStepDistance = 3.5f;
    public float runStepDistance = 5.5f;
    public float runSpeedThreshold = 6.0f;

    [Header("Zemin Sesleri")]
    public AudioClip dirtFootstepClip;
    public AudioClip rockFootstepClip;
    public AudioClip woodFootstepClip;

    [Header("Ses Yükseklikleri")]
    [Range(0f, 1f)] public float dirtVolume = 0.4f;
    [Range(0f, 2f)] public float rockVolume = 1.2f;
    [Range(0f, 1f)] public float woodVolume = 0.4f;
    public float runVolumeMultiplier = 1.4f;

    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceTraveled;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // %100 3D Ses
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Mesafe hesabýný sadece bu karakterin sahibi olan local oyuncu yapar
        if (!IsOwner) return;

        // 1. ZEMÝN KONTROLÜ
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);
        bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 1.2f);

        if (!isGrounded)
        {
            distanceTraveled = 0f;
            lastPosition = transform.position;
            return;
        }

        // 2. YATAY MESAFE HESABI
        Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPosXZ = new Vector3(lastPosition.x, 0, lastPosition.z);

        float distanceThisFrame = Vector3.Distance(currentPosXZ, lastPosXZ);
        distanceTraveled += distanceThisFrame;
        lastPosition = transform.position;

        float currentSpeed = distanceThisFrame / Time.deltaTime;

        if (currentSpeed < 0.1f)
        {
            distanceTraveled = 0f;
            return;
        }

        bool isRunning = currentSpeed > runSpeedThreshold;
        float currentStepDistance = isRunning ? runStepDistance : walkStepDistance;

        // 3. YOKUÞ TOLERANSI
        float slopeAngle = Vector3.Angle(Vector3.up, groundHit.normal);
        if (slopeAngle > 5f)
        {
            currentStepDistance *= Mathf.Lerp(1f, 0.5f, slopeAngle / 45f);
        }

        // 4. ADIM TETÝKLEME KONTROLÜ
        if (distanceTraveled >= currentStepDistance)
        {
            distanceTraveled %= currentStepDistance;
            PlayFootstepSound(isRunning, groundHit);
        }
    }

    private void PlayFootstepSound(bool isRunning, RaycastHit groundHit)
    {
        // Að üzerinden klip gönderemediðimiz için zeminleri sayýlara atýyoruz:
        // 0 = Ahþap (Wood), 1 = Toprak (Dirt), 2 = Taþ (Rock)
        int zeminTuruIndeksi = 0;

        if (groundHit.collider != null && groundHit.collider.TryGetComponent(out Terrain terrain))
        {
            int texIndex = GetDominantTerrainTexture(groundHit.point, terrain);
            if (texIndex == 3)
            {
                zeminTuruIndeksi = 2; // Taþ
            }
            else
            {
                zeminTuruIndeksi = 1; // Toprak
            }
        }

        // 1. Kendi ekranýmýzda hiç að gecikmesi (Ping) beklemeden ANINDA sesi çalýyoruz
        ExecutePlayFootstep(zeminTuruIndeksi, isRunning);

        // 2. Sunucuya "Ben adým attým, diðerlerine de çal" paketini fýrlatýyoruz
        RequestFootstepPlayServerRpc(zeminTuruIndeksi, isRunning);
    }

    [Rpc(SendTo.Server)]
    private void RequestFootstepPlayServerRpc(int zeminTuru, bool isRunning)
    {
        // Sunucu emri alýr ve sesi sadece sahibi OLMAYAN (NotOwner) diðer oyunculara gönderir.
        // Böylece senin bilgisayarýnda ses ikinci kez patlayýp yanký yapmaz.
        BroadcastFootstepToOthersRpc(zeminTuru, isRunning);
    }

    [Rpc(SendTo.NotOwner)]
    private void BroadcastFootstepToOthersRpc(int zeminTuru, bool isRunning)
    {
        // Diðer oyuncularýn bilgisayarlarýnda, senin karakterinin olduðu koordinatta ses tetiklenir
        ExecutePlayFootstep(zeminTuru, isRunning);
    }

    // --- SESÝ FÝZÝKSEL OLARAK HOPARLÖRE GÖNDEREN ANA MOTOR ---
    private void ExecutePlayFootstep(int zeminTuru, bool isRunning)
    {
        AudioClip clipToPlay = woodFootstepClip;
        float baseVolume = woodVolume;

        if (zeminTuru == 1)
        {
            clipToPlay = dirtFootstepClip;
            baseVolume = dirtVolume;
        }
        else if (zeminTuru == 2)
        {
            clipToPlay = rockFootstepClip;
            baseVolume = rockVolume;
        }

        if (clipToPlay != null && audioSource != null)
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
            if (alphamap[0, 0, i] > maxWeight)
            {
                maxWeight = alphamap[0, 0, i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}