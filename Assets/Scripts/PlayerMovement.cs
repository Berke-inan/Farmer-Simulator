using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.0f;
    public float gravity = -9.81f;

    [Header("Traktöre Binme")]
    public float interactionDistance = 3f;

    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;
    private TractorController currentTractor;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Player.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Traktöre Binme Kontrolü
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (currentTractor == null || !currentTractor.IsOccupied)
            {
                CheckAndMountTractor();
            }
            else
            {
                DismountTractor();
            }
        }

        // Eğer traktördeyse işlem yapma
        if (currentTractor != null && currentTractor.IsOccupied)
            return;

        // Yerçekimi Kontrolü
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Girdileri oku
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool isRunning = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        // Fiziksel hareket
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Animasyonları Güncelle
        if (animator != null)
        {
            float multiplier = isRunning ? 2f : 1f;

            // 0.15f Damp Time: Geçişlerin yumuşak (smooth) olmasını sağlar
            animator.SetFloat("Horizontal", moveInput.x * multiplier, 0.15f, Time.deltaTime);
            animator.SetFloat("Vertical", moveInput.y * multiplier, 0.15f, Time.deltaTime);
        }

        // Yerçekimi Uygula
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void CheckAndMountTractor()
    {
        // Yakındaki traktörü ara
        TractorController[] tractors = FindObjectsOfType<TractorController>();

        foreach (TractorController tractor in tractors)
        {
            float distance = Vector3.Distance(transform.position, tractor.transform.position);

            if (distance <= interactionDistance)
            {
                currentTractor = tractor;
                tractor.MountTractor(GetComponent<NetworkObject>());
                return;
            }
        }
    }

    private void DismountTractor()
    {
        if (currentTractor != null)
        {
            currentTractor.DismountTractor();
            currentTractor = null;
        }
    }
}