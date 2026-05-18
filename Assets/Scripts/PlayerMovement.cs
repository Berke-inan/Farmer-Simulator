using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.0f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    private float jumpCooldownTimer = 0f;
    private float jumpCooldownDuration = 0.5f;

    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;
    private PlayerEnergy playerEnergy;

    private InputSystem_Actions controls;
    private Vector2 moveInput;
    private bool isRunning;

    [Header("Uyku Sistemi")]
    private bool isSleeping = false;
    private Bed currentBed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerEnergy = GetComponent<PlayerEnergy>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls = new InputSystem_Actions();
            controls.Player.Jump.started += ctx => Jump();
            controls.Player.Sprint.started += ctx => isRunning = true;
            controls.Player.Sprint.canceled += ctx => isRunning = false;

            if (enabled) controls.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && controls != null)
        {
            controls.Disable();
            controls.Player.Jump.started -= ctx => Jump();
            controls.Player.Sprint.started -= ctx => isRunning = true;
            controls.Player.Sprint.canceled -= ctx => isRunning = false;
        }
    }

    private void OnEnable()
    {
        jumpCooldownTimer = jumpCooldownDuration;
        if (controls != null) controls.Enable();
    }

    private void OnDisable()
    {
        velocity = Vector3.zero;
        moveInput = Vector2.zero;
        isRunning = false;
        if (controls != null) controls.Disable();
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Menü AÇIKSA veya Chat AÇIKSA hareketi/kamerayı dondur
        if (FarmerSimulator.UI.MainMenuController.IsMenuOpen || FarmerSimulator.UI.ChatController.IsChatOpen) return;

        if (isSleeping)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame || !DayNightCycleManager.Instance.IsNight())
            {
                WakeUp();
            }
            return;
        }

        if (jumpCooldownTimer > 0) jumpCooldownTimer -= Time.deltaTime;

        if (controls != null)
        {
            moveInput = controls.Player.Move.ReadValue<Vector2>();
        }

        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        bool canRun = isRunning && moveInput.y > 0;
        if (playerEnergy != null && !playerEnergy.KosabilirMi()) canRun = false;

        float currentSpeed = canRun ? runSpeed : walkSpeed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        Vector3 finalMovement = move * currentSpeed;
        finalMovement.y = velocity.y;

        controller.Move(finalMovement * Time.deltaTime);

        if (animator != null)
        {
            float multiplier = canRun ? 2f : 1f;
            animator.SetFloat("Horizontal", moveInput.x * multiplier, 0.15f, Time.deltaTime);
            animator.SetFloat("Vertical", moveInput.y * multiplier, 0.15f, Time.deltaTime);
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (animator != null) animator.SetBool("isJumping", false);
        }
    }

    private void Jump()
    {
        if (!enabled || isSleeping) return;
        if (playerEnergy != null && !playerEnergy.KosabilirMi()) return;

        if (controller.isGrounded && jumpCooldownTimer <= 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetBool("isJumping", true);
        }
    }

    public void StartSleeping(Bed bed)
    {
        if (isSleeping) return;
        isSleeping = true;
        currentBed = bed;
        moveInput = Vector2.zero;
        velocity = Vector3.zero;
        isRunning = false;

        if (animator != null)
        {
            animator.SetFloat("Horizontal", 0f);
            animator.SetFloat("Vertical", 0f);
            animator.SetBool("isJumping", false);
        }
    }

    public void WakeUp()
    {
        if (!isSleeping) return;
        isSleeping = false;
        if (currentBed != null)
        {
            currentBed.SetBedOccupiedRpc(false);
            currentBed = null;
        }
    }
}