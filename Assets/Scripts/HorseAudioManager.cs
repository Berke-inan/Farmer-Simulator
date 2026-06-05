using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(AudioSource))]
public class HorseAudioManager : NetworkBehaviour
{
    [Header("Ayak Sesleri (Döngü/Loop)")]
    public AudioClip walkClip;
    [Range(0.5f, 2f)] public float walkPitch = 1f;

    public AudioClip gallopClip;
    [Range(0.5f, 2f)] public float gallopPitch = 1f;

    [Header("Bileþen Referanslarý")]
    public AudioSource audioSource;
    public Animator animator;
    public HorseController horseController;

    [Header("Kiþneme Sesleri")]
    public AudioClip[] neighClips;
    public float minNeighInterval = 20f;
    public float maxNeighInterval = 45f;

    private float neighTimer;
    private int currentMovementState = 0;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int eatHash = Animator.StringToHash("IsEating");
    private readonly int groundedHash = Animator.StringToHash("IsGrounded"); // YENÝ EKLENDÝ

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (animator == null) animator = GetComponent<Animator>();
        if (horseController == null) horseController = GetComponent<HorseController>();

        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 35f;
        audioSource.loop = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ResetNeighTimer();
        }
    }

    private void Update()
    {
        HandleMovementAudio();

        if (IsServer && horseController != null && !horseController.isRidden)
        {
            neighTimer -= Time.deltaTime;
            if (neighTimer <= 0f)
            {
                PlayNeighSoundAcrossNetwork();
            }
        }
    }

    private void HandleMovementAudio()
    {
        if (animator == null || audioSource == null) return;

        // --- YENÝ EKLENEN: AT HAVADAYSA SESÝ KES ---
        if (!animator.GetBool(groundedHash))
        {
            if (currentMovementState != 0)
            {
                audioSource.Stop();
                currentMovementState = 0;
            }
            return; // Havada olduðu sürece aþaðýdaki iþlemleri pas geç
        }
        // -------------------------------------------

        float currentAnimSpeed = animator.GetFloat(speedHash);
        int targetState = 0;

        if (currentAnimSpeed >= 0.02f && !animator.GetBool(eatHash))
        {
            targetState = (currentAnimSpeed > 0.25f) ? 2 : 1;
        }

        if (targetState != currentMovementState)
        {
            currentMovementState = targetState;

            if (currentMovementState == 0)
            {
                audioSource.Stop();
            }
            else if (currentMovementState == 1 && walkClip != null)
            {
                audioSource.clip = walkClip;
                audioSource.pitch = walkPitch;
                audioSource.volume = 0.5f;
                audioSource.Play();
            }
            else if (currentMovementState == 2 && gallopClip != null)
            {
                audioSource.clip = gallopClip;
                audioSource.pitch = gallopPitch;
                audioSource.volume = 1.0f;
                audioSource.Play();
            }
        }
    }

    private void PlayNeighSoundAcrossNetwork()
    {
        if (neighClips == null || neighClips.Length == 0) return;

        int randomClipIndex = Random.Range(0, neighClips.Length);
        PlayNeighClientRpc(randomClipIndex);
        ResetNeighTimer();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayNeighClientRpc(int clipIndex)
    {
        if (neighClips != null && clipIndex < neighClips.Length && audioSource != null)
        {
            audioSource.PlayOneShot(neighClips[clipIndex], 0.8f);
        }
    }

    private void ResetNeighTimer()
    {
        neighTimer = Random.Range(minNeighInterval, maxNeighInterval);
    }
}