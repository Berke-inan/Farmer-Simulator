using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Hareket Ayarları")]
    public float walkSpeed = 3.5f;
    public float runSpeed = 6.0f;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;

    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;

    private InputSystem_Actions controls;
    private Vector2 moveInput;
    private bool isRunning;

    // --- EKLENEN KISIM: Cooldown Sayacı ---
    private float jumpCooldown = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    // --- EKLENEN KISIM: Script her aktif edildiğinde (traktörden inince) çalışır ---
    private void OnEnable()
    {
        jumpCooldown = 0.2f;     // 0.2 saniyelik zıplama yasağı koyar
        velocity = Vector3.zero; // Birikmiş düşüş hızını sıfırlar (yere saplanmayı önler)
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            controls = new InputSystem_Actions();
            controls.Enable();

            controls.Player.Jump.started += ctx => Jump();

            controls.Player.Sprint.started += ctx => isRunning = true;
            controls.Player.Sprint.canceled += ctx => isRunning = false;
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

    private void Update()
    {
        if (!IsOwner) return;

        // --- EKLENEN KISIM: Cooldown süresini geriye doğru saydır ---
        if (jumpCooldown > 0)
        {
            jumpCooldown -= Time.deltaTime;
        }

        if (controls != null)
        {
            moveInput = controls.Player.Move.ReadValue<Vector2>();
        }

        // 1. Yerçekimi ve Zemin Kontrolü
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Yere yapışmayı sağlar
        }
        velocity.y += gravity * Time.deltaTime;

        // 2. Yatay Hareket Hesaplaması
        float currentSpeed = (isRunning && moveInput.y > 0) ? runSpeed : walkSpeed;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;

        // 3. Vektörleri Birleştirme
        Vector3 finalMovement = move * currentSpeed;
        finalMovement.y = velocity.y; // Yatay harekete dikey (Y) hızını ekle

        // 4. TEK BİR Move Çağrısı
        controller.Move(finalMovement * Time.deltaTime);

        // 5. Animasyonlar
        if (animator != null)
        {
            float multiplier = isRunning ? 2f : 1f;
            animator.SetFloat("Horizontal", moveInput.x * multiplier, 0.15f, Time.deltaTime);
            animator.SetFloat("Vertical", moveInput.y * multiplier, 0.15f, Time.deltaTime);
        }

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;

            if (animator != null)
                animator.SetBool("isJumping", false);
        }
    }

    private void Jump()
    {
        if (controller.isGrounded && jumpCooldown <= 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
                animator.SetBool("isJumping", true);
        }
    }
}