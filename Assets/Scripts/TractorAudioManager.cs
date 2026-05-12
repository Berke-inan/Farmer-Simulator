using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(TractorController), typeof(Rigidbody))]
public class TractorAudioManager : NetworkBehaviour
{
    [Header("Döngü Sesleri (Aðdaki Herkes Duyar)")]
    public AudioClip idleSound;
    public AudioClip movingSound;
    public AudioClip reverseBeepSound;

    [Header("Tepki Sesleri (Sadece Sürücü Duyar)")]
    public AudioClip engineStartSound;
    public AudioClip engineStopSound;
    public AudioClip initialAccelSound;
    public AudioClip throttleResumeSound;
    public AudioClip decelerationSound;

    [Header("Ses Ayarlarý")]
    public float masterVolume = 1f;
    public float fadeSpeed = 5f;
    public float maxPitch = 1.5f;
    public float maxSpeedForPitch = 70f;

    // Arka Plandaki Hoparlörler (Kanallar)
    private AudioSource idleSource;
    private AudioSource movingSource;
    private AudioSource reverseSource;
    private AudioSource reactionSource; // Eskiden oneShotSource idi, artýk üst üste binmeyen tekli kanalýmýz.

    private Rigidbody rb;
    private TractorController tractorController;
    private TractorFuelSystem fuelSystem;

    // Durum Takip Deðiþkenleri
    private bool wasEngineRunning = false;
    private bool wasPressingGas = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();
        fuelSystem = GetComponent<TractorFuelSystem>();

        // Hoparlörleri oluþtur
        idleSource = CreateAudioSource(idleSound, true);
        movingSource = CreateAudioSource(movingSound, true);
        reverseSource = CreateAudioSource(reverseBeepSound, true);

        reactionSource = CreateAudioSource(null, false); // Tek seferlik sesler için döngüsüz kanal
    }

    private AudioSource CreateAudioSource(AudioClip clip, bool loop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.spatialBlend = 1f;
        source.minDistance = 5f;
        source.maxDistance = 50f;
        source.volume = 0f;
        return source;
    }

    private void Update()
    {
        bool isEngineOn = fuelSystem != null && fuelSystem.isEngineRunning.Value;

        // 1. Motorun Açýlýþ/Kapanýþ Kontrolü
        HandleEnginePower(isEngineOn);

        // Eðer motor kapalýysa alt kýsýmlarý (gaz, hýz hesaplamalarý) HÝÇ ÇALIÞTIRMA (Performans tasarrufu)
        if (!isEngineOn) return;

        // 2. Fiziksel Verileri Oku
        float speed = rb.linearVelocity.magnitude * 3.6f;
        bool isPressingGas = Mathf.Abs(tractorController.CurrentGasInput) > 0.05f;
        bool isReversing = Vector3.Dot(rb.linearVelocity, transform.forward) < -0.5f;

        // 3. Sürücü Tepkilerini Ýþle (Gaz verme, gazdan çekme)
        if (tractorController.IsDrivenByMe)
        {
            HandleReactions(speed, isPressingGas);
        }

        // 4. Arka Plan Motor Gürültülerini Ýþle (Rölanti, Yürüme, Geri vites)
        HandleLoopSounds(speed, isPressingGas, isReversing);
    }

    // --- TEMÝZ KOD (CLEAN CODE) MODÜLLERÝ ---

    private void HandleEnginePower(bool isEngineOn)
    {
        // MOTOR KAPATILDIÐI AN
        if (!isEngineOn && wasEngineRunning)
        {
            StopAllSources();
            PlayReaction(engineStopSound); // Anýnda kapanýþ sesini çal
            wasEngineRunning = false;
            wasPressingGas = false;
        }
        // MOTOR ÇALIÞTIRILDIÐI AN
        else if (isEngineOn && !wasEngineRunning)
        {
            StopAllSources(); // Önceki kapanýþ veya yarým kalan sesleri HÝÇ ACIMADAN KES
            PlayReaction(engineStartSound);

            idleSource.Play();
            movingSource.Play();
            reverseSource.Play();

            wasEngineRunning = true;
        }
        // GÜVENLÝK: Motor kapalýyken çalan kapanýþ sesi 3 saniyeyi geçerse zorla sustur
        else if (!isEngineOn && !wasEngineRunning)
        {
            if (reactionSource.isPlaying && reactionSource.time > 3f)
            {
                reactionSource.Stop();
            }
        }
    }

    private void HandleReactions(float speed, bool isPressingGas)
    {
        // Gaza ÞU AN basýldý
        if (isPressingGas && !wasPressingGas)
        {
            if (speed < 3f) PlayReaction(initialAccelSound);
            else PlayReaction(throttleResumeSound);
        }
        // Gaz ÞU AN býrakýldý
        else if (!isPressingGas && wasPressingGas)
        {
            if (speed > 3f) PlayReaction(decelerationSound);
        }

        wasPressingGas = isPressingGas;
    }

    private void HandleLoopSounds(float speed, bool isPressingGas, bool isReversing)
    {
        float targetIdle = (speed < 1f && !isPressingGas) ? masterVolume : 0f;
        float targetMoving = (speed >= 1f || isPressingGas) ? masterVolume : 0f;
        float targetReverse = (isReversing && targetMoving > 0f) ? masterVolume : 0f;

        // Ses seviyelerini yumuþakça ayarla (Crossfade)
        idleSource.volume = Mathf.Lerp(idleSource.volume, targetIdle, Time.deltaTime * fadeSpeed);
        movingSource.volume = Mathf.Lerp(movingSource.volume, targetMoving, Time.deltaTime * fadeSpeed);
        reverseSource.volume = Mathf.Lerp(reverseSource.volume, targetReverse, Time.deltaTime * fadeSpeed * 2f);

        // Motor Baðýrmasý (Pitch ayarý)
        movingSource.pitch = 1f + ((speed / maxSpeedForPitch) * (maxPitch - 1f));
        idleSource.pitch = isPressingGas ? 1.15f : 1f;
    }

    // Bu fonksiyon hayat kurtarýr: Verilen sesi çalarken, kanalda eski ne varsa anýnda ezer geçer.
    private void PlayReaction(AudioClip clip)
    {
        if (clip == null) return;
        reactionSource.clip = clip;
        reactionSource.volume = masterVolume;
        reactionSource.Play();
    }

    private void StopAllSources()
    {
        idleSource.Stop();
        movingSource.Stop();
        reverseSource.Stop();
        reactionSource.Stop();
    }
}