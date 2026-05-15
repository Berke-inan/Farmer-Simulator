using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class RandomAnimalSound : NetworkBehaviour
{
    [Header("Ses Dosyalarý")]
    [Tooltip("Buraya istediðin kadar (1, 2, 3...) ses ekleyebilirsin.")]
    public AudioClip[] animalSounds;

    [Header("Zaman Ayarlarý")]
    [Tooltip("Ýki ses arasýndaki EN AZ bekleme süresi (Saniye)")]
    public float minWaitTime = 10f;
    [Tooltip("Ýki ses arasýndaki EN FAZLA bekleme süresi (Saniye)")]
    public float maxWaitTime = 30f;

    private AudioSource audioSource;
    private float timer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Sesin oyun dünyasýnda 3 boyutlu (uzaklaþtýkça azalan) olmasý için zorunlu ayar
        audioSource.spatialBlend = 1f;
        audioSource.playOnAwake = false;
    }

    public override void OnNetworkSpawn()
    {
        // Zamanlayýcýyý sadece sunucu (Server) baþlatýr
        if (IsServer)
        {
            SetRandomTimer();
        }
    }

    private void Update()
    {
        // Zamaný sadece sunucu sayar. Böylece herkes sesi AYNI ANDA duyar.
        if (!IsServer || animalSounds.Length == 0) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            // Listeden rastgele bir ses seç
            int randomIndex = Random.Range(0, animalSounds.Length);

            // Tüm oyunculara bu sesi çalmalarýný söyle
            PlaySoundClientRpc(randomIndex);

            // ZAMANI SIFIRLA: Rastgele süre + Çalýnan sesin kendi uzunluðu
            // (Böylece ses bitmeden asla yeni ses çalamaz, üst üste binmez!)
            float clipLength = animalSounds[randomIndex].length;
            SetRandomTimer(clipLength);
        }
    }

    private void SetRandomTimer(float extraWait = 0f)
    {
        timer = Random.Range(minWaitTime, maxWaitTime) + extraWait;
    }

    [ClientRpc]
    private void PlaySoundClientRpc(int soundIndex)
    {
        // Eðer ses listemizde böyle bir index varsa sesi çal
        if (soundIndex >= 0 && soundIndex < animalSounds.Length)
        {
            audioSource.PlayOneShot(animalSounds[soundIndex]);
        }
    }
}