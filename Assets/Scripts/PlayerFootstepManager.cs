using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepManager : NetworkBehaviour
{
    [Header("Mesafe Ayarlarý")]
    public float walkStepDistance = 1.6f; // Yürürken adým aralýðý
    public float runStepDistance = 3.2f;  // Koþarken adým aralýðý

    [Header("Ses ve Zamanlama")]
    public AudioClip footstepClip;
    [Range(0f, 1f)] public float volume = 0.4f;
    public float minTimeBetweenSteps = 0.25f; // Ýki adým arasýnda en az ne kadar süre geçmeli? (Makinalý tüfek sesini engeller)

    private AudioSource audioSource;
    private Vector3 lastPosition;
    private float distanceTraveled;
    private float lastStepTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = 15f;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // 1. Bu frame ne kadar yol aldýk?
        float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
        distanceTraveled += distanceThisFrame;
        lastPosition = transform.position;

        // 2. Karakterin þu anki hýzýný ölç (Saniyede kaç metre gidiyor?)
        float currentSpeed = distanceThisFrame / Time.deltaTime;

        // 3. Hýza göre adým mesafesini seç (Eðer hýz 4'ten büyükse koþuyor kabul et)
        float currentStepDistance = (currentSpeed > 4f) ? runStepDistance : walkStepDistance;

        // 4. Eðer yeterli yol gidildiyse VE son adýmdan sonra yeterli süre geçtiyse:
        if (distanceTraveled >= currentStepDistance && Time.time - lastStepTime >= minTimeBetweenSteps)
        {
            distanceTraveled = 0f;
            lastStepTime = Time.time;
            PlayFootstepSound();
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepClip != null && audioSource != null)
        {
            // Koþarken ses biraz daha kalýn ve yüksek çýksýn hilesi
            audioSource.pitch = Random.Range(0.85f, 1.1f);
            audioSource.PlayOneShot(footstepClip, volume);
        }
    }
}