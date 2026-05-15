using UnityEngine;
using Unity.Netcode;

public class DayNightAudioManager : NetworkBehaviour
{
    [Header("Sürekli Ortam Sesleri (Loop)")]
    public AudioSource dayAudioSource;
    public AudioSource nightAudioSource;

    [Header("Rastgele Gece Sesleri (Kurt, Baykuþ)")]
    public AudioSource randomSfxSource;
    public AudioClip[] nightRandomClips;
    public float minWaitTime = 15f;
    public float maxWaitTime = 40f;

    [Header("Geçiþ Ayarlarý")]
    [Tooltip("Gündüz/Gece seslerinin ne kadar yavaþ birbirine geçeceðini belirler")]
    public float dayNightFadeSpeed = 0.5f;

    // Evin içine girince bu deðer HouseAudioZone tarafýndan düþürülecek
    [HideInInspector] public float houseVolumeMultiplier = 1.0f;

    private float dayWeight = 1f;
    private float nightWeight = 0f;
    private float randomTimer;

    public override void OnNetworkSpawn()
    {
        if (IsServer) SetRandomTimer();
    }

    private void Update()
    {
        // 1. Senin DayNightCycleManager kodundan saatin gece olup olmadýðýný OTOMATÝK öðreniyoruz!
        bool isNight = false;
        if (DayNightCycleManager.Instance != null)
        {
            isNight = DayNightCycleManager.Instance.IsNight();
        }

        // 2. Gece/Gündüz aðýrlýðýný hesapla (Yumuþak geçiþ matematiði)
        float targetDay = isNight ? 0f : 1f;
        float targetNight = isNight ? 1f : 0f;

        dayWeight = Mathf.MoveTowards(dayWeight, targetDay, Time.deltaTime * dayNightFadeSpeed);
        nightWeight = Mathf.MoveTowards(nightWeight, targetNight, Time.deltaTime * dayNightFadeSpeed);

        // 3. Nihai ses seviyelerini uygula (Evin içine girince çarpan düþer ve tüm sesler kýsýlýr)
        if (dayAudioSource != null) dayAudioSource.volume = dayWeight * houseVolumeMultiplier;
        if (nightAudioSource != null) nightAudioSource.volume = nightWeight * houseVolumeMultiplier;

        // 4. Rastgele Gece Sesleri (Sadece Gece ve sadece Server sayar)
        if (isNight && IsServer && nightRandomClips != null && nightRandomClips.Length > 0)
        {
            randomTimer -= Time.deltaTime;
            if (randomTimer <= 0f)
            {
                int randomIndex = Random.Range(0, nightRandomClips.Length);
                PlayRandomNightSoundClientRpc(randomIndex);

                float clipLength = nightRandomClips[randomIndex].length;
                SetRandomTimer(clipLength);
            }
        }
    }

    private void SetRandomTimer(float extraWait = 0f)
    {
        randomTimer = Random.Range(minWaitTime, maxWaitTime) + extraWait;
    }

    [ClientRpc]
    private void PlayRandomNightSoundClientRpc(int clipIndex)
    {
        if (clipIndex >= 0 && clipIndex < nightRandomClips.Length && randomSfxSource != null)
        {
            // Kurt/Baykuþ sesini de evin içindeysen boðuk, dýþarýdaysan net duymaný saðlar
            randomSfxSource.PlayOneShot(nightRandomClips[clipIndex], houseVolumeMultiplier);
        }
    }
}