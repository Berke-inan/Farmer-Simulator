using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(TractorController), typeof(Rigidbody))]
public class TractorAudioManager : NetworkBehaviour
{
    [Header("Döngü Sesleri (Sürekli Çalanlar)")]
    public AudioClip idleSound;         // Rölanti
    public AudioClip movingSound;       // Sabit hýzda gidiþ (Asýl ses)
    public AudioClip reverseBeepSound;  // Geri geri bip bip

    [Header("Tepki Sesleri (Bir Kez Çalanlar)")]
    public AudioClip engineStartSound;    // Ýlk biniþ (Kontak açma)
    public AudioClip initialAccelSound;   // Ýlk kalkýþ / Gaza ilk basma
    public AudioClip throttleResumeSound; // Giderken tekrar gaza basma
    public AudioClip decelerationSound;   // Ýlerlerken gazý býrakma

    [Header("Ses Ayarlarý")]
    public float masterVolume = 1f;
    public float fadeSpeed = 5f;        // Sesler arasý geçiþ hýzý
    public float maxPitch = 1.5f;       // Son hýzda motor ne kadar baðýrsýn?
    public float maxSpeedForPitch = 70f;// Traktörün son hýzý

    // Arka Plandaki Hoparlörler
    private AudioSource idleSource;
    private AudioSource movingSource;
    private AudioSource reverseSource;
    private AudioSource oneShotSource;  // Tepkiler için anlýk hoparlör

    private Rigidbody rb;
    private TractorController tractorController;

    // Durum Takip Deðiþkenleri
    private bool wasOccupied = false;
    private bool wasPressingGas = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();

        // Sürekli çalan döngü hoparlörlerini otomatik oluþtur
        idleSource = CreateLoopSource(idleSound);
        movingSource = CreateLoopSource(movingSound);
        reverseSource = CreateLoopSource(reverseBeepSound);

        // Anlýk tepki hoparlörünü oluþtur
        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.spatialBlend = 1f; // 3D Ses
        oneShotSource.minDistance = 5f;
        oneShotSource.maxDistance = 50f;
    }

    private AudioSource CreateLoopSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 1f;
        source.minDistance = 5f;
        source.maxDistance = 50f;
        source.volume = 0f; // Baþta sessiz
        if (clip != null) source.Play();
        return source;
    }

    private void Update()
    {
        // 1. MOTOR KAPALIYKEN
        if (!tractorController.IsOccupied)
        {
            FadeOutAll();
            wasOccupied = false;
            wasPressingGas = false;
            return;
        }

        // 2. KONTAK AÇMA (Ýlk Biniþ)
        if (!wasOccupied)
        {
            if (engineStartSound != null) oneShotSource.PlayOneShot(engineStartSound, masterVolume);
            wasOccupied = true;
        }

        // Fiziksel Verileri Oku
        float currentSpeed = rb.linearVelocity.magnitude * 3.6f; // km/h
        float gasInput = tractorController.CurrentGasInput;
        bool isPressingGas = Mathf.Abs(gasInput) > 0.05f;

        // Geri vites kontrolü (Traktörün burnu ile gidiþ yönü zýtsa geri gidiyordur)
        float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool isReversing = forwardSpeed < -0.5f;

        // =========================================================
        // 3. TEPKÝ SESLERÝ (SADECE SÜRÜCÜ DUYAR)
        // =========================================================
        if (tractorController.IsDrivenByMe)
        {
            if (isPressingGas && !wasPressingGas) // Gaza ÞU AN basýldý
            {
                if (currentSpeed < 3f)
                {
                    // Dururken gaza bastý (Ýlk Kalkýþ)
                    if (initialAccelSound != null) oneShotSource.PlayOneShot(initialAccelSound, masterVolume);
                }
                else
                {
                    // Giderken tekrar gaza yüklendi
                    if (throttleResumeSound != null) oneShotSource.PlayOneShot(throttleResumeSound, masterVolume * 0.8f);
                }
            }
            else if (!isPressingGas && wasPressingGas) // Gaz ÞU AN býrakýldý
            {
                if (currentSpeed > 3f)
                {
                    // Ýlerlerken gazý kesti (Motor kompresörü/Yýðýlma sesi)
                    if (decelerationSound != null) oneShotSource.PlayOneShot(decelerationSound, masterVolume * 0.8f);
                }
            }
        }
        wasPressingGas = isPressingGas; // Durumu hafýzaya al

        // =========================================================
        // 4. DÖNGÜ SESLERÝ VE CROSSFADE (HERKES DUYAR)
        // =========================================================
        float targetIdle = 0f;
        float targetMoving = 0f;
        float targetReverse = 0f;

        if (currentSpeed < 1f && !isPressingGas)
        {
            // Traktör duruyor ve gaza basýlmýyor -> Sadece Rölanti
            targetIdle = masterVolume;
        }
        else
        {
            // Traktör hareket ediyor VEYA gaza basýlýyor -> Asýl yürüme sesi
            targetMoving = masterVolume;

            if (isReversing)
            {
                // Geri gidiyorsa Bip Bip sesini aç
                targetReverse = masterVolume;
            }
        }

        // Hacimleri hedefe doðru yumuþakça kaydýr (Crossfade)
        idleSource.volume = Mathf.Lerp(idleSource.volume, targetIdle, Time.deltaTime * fadeSpeed);
        movingSource.volume = Mathf.Lerp(movingSource.volume, targetMoving, Time.deltaTime * fadeSpeed);
        reverseSource.volume = Mathf.Lerp(reverseSource.volume, targetReverse, Time.deltaTime * fadeSpeed * 2f); // Bip sesi biraz daha hýzlý girsin

        // =========================================================
        // 5. MOTOR BAÐIRMASI (PITCH)
        // =========================================================
        // Hýz arttýkça ana motor sesi incelir
        float pitchOffset = (currentSpeed / maxSpeedForPitch) * (maxPitch - 1f);
        movingSource.pitch = 1f + pitchOffset;

        // Rölantideyken gaza hafif dokunursa rölanti sesi de hafif incelsin
        idleSource.pitch = isPressingGas ? 1.15f : 1f;
    }

    private void FadeOutAll()
    {
        // Traktörden inilince sesler býçak gibi kesilmez, yavaþça susar
        idleSource.volume = Mathf.Lerp(idleSource.volume, 0f, Time.deltaTime * fadeSpeed);
        movingSource.volume = Mathf.Lerp(movingSource.volume, 0f, Time.deltaTime * fadeSpeed);
        reverseSource.volume = Mathf.Lerp(reverseSource.volume, 0f, Time.deltaTime * fadeSpeed);
    }
}