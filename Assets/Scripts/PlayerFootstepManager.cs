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
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // 1. ZEMÝN KONTROLÜ (Havadaysak ses çalma ve mesafe sayma)
        Vector3 rayStart = transform.position + (Vector3.up * 0.5f);
        bool isGrounded = Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, 1.2f);

        if (!isGrounded)
        {
            distanceTraveled = 0f; // Havada sayacý sýfýrla
            lastPosition = transform.position; // Pozisyonu güncelle ki yere inince anýnda çalmasýn
            return;
        }

        // 2. YATAY MESAFE HESABI (Sadece X ve Z ekseni. Yokuþtaki hatalarý önler)
        Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 lastPosXZ = new Vector3(lastPosition.x, 0, lastPosition.z);

        float distanceThisFrame = Vector3.Distance(currentPosXZ, lastPosXZ);
        distanceTraveled += distanceThisFrame;
        lastPosition = transform.position;

        // Yatay hýzý hesapla
        float currentSpeed = distanceThisFrame / Time.deltaTime;

        // Karakter duruyorsa iþlem yapma
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
            // Yokuþ dikleþtikçe adým kotasýný daralt (yavaþlamayý telafi eder)
            currentStepDistance *= Mathf.Lerp(1f, 0.5f, slopeAngle / 45f);
        }

        // 4. SESÝ ÇAL (Artýk zaman kýsýtlamasýna gerek yok, mesafe kusursuz çalýþýr)
        if (distanceTraveled >= currentStepDistance)
        {
            // ÇÖZÜM: Sayacý 0 yapma! Artan küsuratý (örneðin 3.6 - 3.5 = 0.1) koru.
            // Bu sayede ritim sekmesi/gecikmesi yaþanmaz.
            distanceTraveled %= currentStepDistance;

            // Çarptýðýmýz zemin verisini (groundHit) fonksiyona yolluyoruz ki tekrar lazer atmasýn
            PlayFootstepSound(isRunning, groundHit);
        }
    }

    private void PlayFootstepSound(bool isRunning, RaycastHit groundHit)
    {
        AudioClip clipToPlay = woodFootstepClip; // Varsayýlan Ses
        float baseVolume = woodVolume;

        // Hangi zeminde yürüdüðümüzü bul
        if (groundHit.collider.TryGetComponent(out Terrain terrain))
        {
            int texIndex = GetDominantTerrainTexture(groundHit.point, terrain);
            if (texIndex == 3) // Taþ zemin indeksi
            {
                clipToPlay = rockFootstepClip;
                baseVolume = rockVolume;
            }
            else
            {
                clipToPlay = dirtFootstepClip;
                baseVolume = dirtVolume;
            }
        }

        // Sesi Çal
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
            if (alphamap[0, 0, i] > maxWeight)
            {
                maxWeight = alphamap[0, 0, i];
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}