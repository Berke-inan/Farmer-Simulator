using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class HorseController : NetworkBehaviour, IInteractable
{
    public enum AnimalState { Idle, Wander, Eat, Panic }

    [Header("Durum (State)")]
    public AnimalState currentState = AnimalState.Idle;
    public bool isRidden = false;

    [Header("Normal Gezinme Ayarlarý")]
    public float wanderRadius = 25f;
    public float minWaitTime = 4f;
    public float maxWaitTime = 8f;
    public float walkSpeed = 3.5f;

    [Header("Korku / Koþma Ayarlarý")]
    public float panicSpeed = 20f;
    public float panicDuration = 10f;
    public float panicEscapeRadius = 50f;

    [Header("Binicilik ve Direksiyon")]
    public Transform driverSeat;
    public float turnSweepingSpeed = 3.0f;
    public float mouseDeadzoneAngle = 15f;

    [Header("Çarpýþma / Radar Ayarý")]
    public float horseHeadLength = 4.0f; // Burun mesafesini 2 metreye çýkardým

    [Header("Zýplama ve Yerçekimi")]
    public float jumpForce = 6f;
    public float gravity = -15f;

    private float verticalVelocity = 0f;
    private bool isGrounded = true;

    private NavMeshAgent agent;
    private Animator animator;
    private float stateTimer;
    private float currentSpeed = 0f;
    private Vector3 smoothedNavDirection;

    private NetworkObject currentDriver;
    private InputSystem_Actions inputActions;
    public bool IsOccupied => currentDriver != null;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int turnHash = Animator.StringToHash("Turn");
    private readonly int eatHash = Animator.StringToHash("IsEating");
    private readonly int jumpHash = Animator.StringToHash("Jump");
    private readonly int groundedHash = Animator.StringToHash("IsGrounded");
    private readonly int animMultiplierHash = Animator.StringToHash("AnimMultiplier");
    private readonly int verticalVelHash = Animator.StringToHash("VerticalVelocity");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        inputActions = new InputSystem_Actions();

        agent.updatePosition = false;
        agent.updateRotation = false;

        smoothedNavDirection = transform.forward;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer && !IsOwner) agent.enabled = false;
    }

    private void Update()
    {
        if (isRidden)
        {
            if (IsOwner && currentDriver != null) HandleRidingMovementLocal();
            return;
        }

        if (!IsServer) return;

        HandleStateMachine();
        CalculateAdvancedMovement();
    }

    private void LateUpdate()
    {
        if (isRidden && currentDriver != null && driverSeat != null)
        {
            currentDriver.transform.position = driverSeat.position;
            currentDriver.transform.rotation = driverSeat.rotation;
        }
    }

    public void Interact(NetworkObject interactor)
    {
        if (!IsOccupied) MountHorseServerRpc(interactor.NetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MountHorseServerRpc(ulong playerId)
    {
        if (IsOccupied) return;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            GetComponent<NetworkObject>().ChangeOwnership(playerObj.OwnerClientId);
            playerObj.TrySetParent(transform);
            isRidden = true;
            MountHorseClientRpc(playerId);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void MountHorseClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            currentDriver = playerObj;
            playerObj.transform.position = driverSeat.position;
            playerObj.transform.rotation = driverSeat.rotation;
            isRidden = true;
            TogglePlayerComponents(playerObj, false);

            if (playerObj.IsOwner)
            {
                inputActions.Player.Enable();
                inputActions.Player.Interact.started += OnInteractPressed;
            }
        }
    }

    private void OnInteractPressed(InputAction.CallbackContext context)
    {
        if (IsOccupied && IsOwner) DismountHorseServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void DismountHorseServerRpc()
    {
        if (currentDriver != null)
        {
            GetComponent<NetworkObject>().RemoveOwnership();
            currentDriver.TryRemoveParent();
            isRidden = false;
            DismountHorseClientRpc();
        }
    }

    [Rpc(SendTo.Everyone)]
    private void DismountHorseClientRpc()
    {
        if (currentDriver != null)
        {
            Vector3 safeLeft = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.left;
            Vector3 targetPosition = transform.position + (safeLeft * 2.0f);

            currentDriver.transform.position = targetPosition;
            currentDriver.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            if (currentDriver.IsOwner)
            {
                inputActions.Player.Interact.started -= OnInteractPressed;
                inputActions.Player.Disable();
            }

            TogglePlayerComponents(currentDriver, true);
            currentDriver = null;
            isRidden = false;

            UpdateAnimator(0f, 0f, true, 1f, 0f);
            currentSpeed = 0f;
            verticalVelocity = 0f;
            smoothedNavDirection = transform.forward;
        }
    }

    // --- KRÝTÝK DEÐÝÞÝKLÝK: YEMEK YEME ANÝMASYONUNU BÝTÝRME ---
    private void TogglePlayerComponents(NetworkObject player, bool state)
    {
        if (player.TryGetComponent(out PlayerMovement movement)) movement.enabled = state;
        if (player.TryGetComponent(out CharacterController characterController)) characterController.enabled = state;
        if (player.TryGetComponent(out PlayerInteractor interactor)) interactor.enabled = state;

        if (player.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = !state;
            rb.useGravity = state;
        }

        // ATA BÝNDÝÐÝMÝZ AN YEMEÐÝ KES
        if (!state)
        {
            animator.SetBool(eatHash, false);
        }

        Animator pAnimator = player.GetComponentInChildren<Animator>();
        if (pAnimator != null) pAnimator.SetBool("isDriving", !state);

        if (player.IsOwner)
        {
            if (player.TryGetComponent(out PlayerInventory inventory)) inventory.SetHolstered(!state);
            if (player.TryGetComponent(out PlayerCameraController camController)) camController.isRiding = !state;
        }
    }

    // ==========================================
    // SÜRÜÞ SÝSTEMÝ
    // ==========================================
    private void HandleRidingMovementLocal()
    {
        float verticalInput = inputActions.Player.GasBrake.ReadValue<float>();
        float horizontalInput = inputActions.Player.Steering.ReadValue<float>();
        bool isSprinting = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        float targetSpeed = 0f;
        if (verticalInput > 0.1f) targetSpeed = isSprinting ? panicSpeed : walkSpeed;
        else if (verticalInput < -0.1f) targetSpeed = -walkSpeed;

        float groundHeight = transform.position.y;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            groundHeight = navHit.position.y;
        }

        verticalVelocity += gravity * Time.deltaTime;
        float nextY = transform.position.y + (verticalVelocity * Time.deltaTime);

        if (nextY <= groundHeight)
        {
            isGrounded = true;
            verticalVelocity = -2f;
            nextY = groundHeight;

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 2.5f);

            if (jumpPressed && currentSpeed > 2.0f)
            {
                verticalVelocity = jumpForce;
                isGrounded = false;
                currentSpeed = panicSpeed * 1.2f;
                animator.SetTrigger(jumpHash);
                PlayJumpAnimationServerRpc();
            }
        }
        else
        {
            isGrounded = false;
        }

        Transform camTransform = Camera.main.transform;
        Vector3 camForward = camTransform.forward;
        camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = camTransform.right;
        camRight.y = 0f; camRight.Normalize();

        Vector3 movementXZ = Vector3.zero;
        float turnRatio = 0f;

        if (currentSpeed > 0.1f)
        {
            float angleToCam = Vector3.SignedAngle(transform.forward, camForward, Vector3.up);
            if (Mathf.Abs(angleToCam) <= mouseDeadzoneAngle && Mathf.Abs(horizontalInput) < 0.1f)
            {
                camForward = transform.forward;
            }

            Vector3 inputDirection = camForward + (camRight * horizontalInput * 1.5f);
            inputDirection.y = 0f;

            if (inputDirection.sqrMagnitude > 0.1f)
            {
                inputDirection.Normalize();
                smoothedNavDirection = Vector3.Lerp(smoothedNavDirection, inputDirection, Time.deltaTime * turnSweepingSpeed);
                smoothedNavDirection.Normalize();

                float angleDiff = Vector3.SignedAngle(transform.forward, smoothedNavDirection, Vector3.up);
                turnRatio = Mathf.Clamp(angleDiff / 30f, -1f, 1f);

                float turnSpeed = 45f + (Mathf.Abs(horizontalInput) * 40f);
                Quaternion lookRot = Quaternion.LookRotation(smoothedNavDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
            }

            movementXZ = transform.forward * currentSpeed;
        }
        else if (currentSpeed < -0.1f)
        {
            turnRatio = horizontalInput;
            float inPlaceTurnSpeed = 45f;
            transform.Rotate(Vector3.up, horizontalInput * inPlaceTurnSpeed * Time.deltaTime);
            smoothedNavDirection = transform.forward;
            movementXZ = transform.forward * currentSpeed;
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            turnRatio = horizontalInput;
            float inPlaceTurnSpeed = 45f;
            transform.Rotate(Vector3.up, horizontalInput * inPlaceTurnSpeed * Time.deltaTime);
            smoothedNavDirection = transform.forward;
        }
        else
        {
            turnRatio = 0f;
            smoothedNavDirection = transform.forward;
        }

        // Radar Kontrolü
        Vector3 targetPosition = transform.position + (movementXZ * Time.deltaTime);
        bool hitWall = false;
        if (currentSpeed > 0.1f)
        {
            if (agent.Raycast(transform.position + (transform.forward * horseHeadLength), out _))
            {
                hitWall = true;
                targetPosition = transform.position;
            }
        }

        Vector3 finalPosition = targetPosition;
        finalPosition.y = nextY;
        transform.position = finalPosition;
        agent.nextPosition = transform.position;

        float speedRatio = 0f;
        float currentAnimMultiplier = 1f;

        if (currentSpeed < -0.1f)
        {
            speedRatio = walkSpeed / panicSpeed;
            currentAnimMultiplier = -1f;
        }
        else
        {
            speedRatio = Mathf.Clamp(currentSpeed / panicSpeed, 0f, 1f);
            currentAnimMultiplier = 1f;
        }

        if (hitWall) { speedRatio = 0f; currentSpeed = 0f; }

        UpdateAnimator(speedRatio, turnRatio, isGrounded, currentAnimMultiplier, verticalVelocity);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void PlayJumpAnimationServerRpc()
    {
        PlayJumpAnimationClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayJumpAnimationClientRpc()
    {
        if (!IsOwner) animator.SetTrigger(jumpHash);
    }

    private void HandleStateMachine()
    {
        stateTimer -= Time.deltaTime;
        switch (currentState)
        {
            case AnimalState.Idle:
            case AnimalState.Eat:
                if (stateTimer <= 0f) ChooseNextState();
                break;
            case AnimalState.Wander:
                if (!agent.pathPending && Vector3.Distance(transform.position, agent.destination) <= 1.5f) ChooseNextState();
                else if (stateTimer <= -5f) ChooseNextState();
                break;
            case AnimalState.Panic:
                if (stateTimer <= 0f) ChooseNextState();
                break;
        }
    }

    private void ChooseNextState()
    {
        int randomVal = Random.Range(0, 100);
        if (randomVal < 20) currentState = AnimalState.Idle;
        else if (randomVal < 40) currentState = AnimalState.Eat;
        else currentState = AnimalState.Wander;

        stateTimer = Random.Range(minWaitTime, maxWaitTime);
        animator.SetBool(eatHash, currentState == AnimalState.Eat);
        if (currentState == AnimalState.Wander) SetRandomDestination(transform.position, wanderRadius);
    }

    private void SetRandomDestination(Vector3 center, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, 1)) agent.SetDestination(hit.position);
    }

    private void CalculateAdvancedMovement()
    {
        float targetSpeed = 0f;
        if (currentState == AnimalState.Wander) targetSpeed = walkSpeed;
        else if (currentState == AnimalState.Panic) targetSpeed = panicSpeed;

        float groundHeight = transform.position.y;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 2f, NavMesh.AllAreas)) groundHeight = navHit.position.y;

        verticalVelocity += gravity * Time.deltaTime;
        float nextY = transform.position.y + (verticalVelocity * Time.deltaTime);

        if (nextY <= groundHeight)
        {
            isGrounded = true;
            verticalVelocity = -2f;
            nextY = groundHeight;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 2f);
        }
        else
        {
            isGrounded = false;
        }

        float speedRatio = Mathf.Clamp(currentSpeed / panicSpeed, 0f, 1f);
        Vector3 movementXZ = Vector3.zero;

        if (currentSpeed > 0.1f && agent.hasPath)
        {
            Vector3 targetDir = agent.steeringTarget - transform.position;
            targetDir.y = 0f;
            float turnRatio = 0f;

            if (targetDir.sqrMagnitude > 0.1f)
            {
                targetDir.Normalize();
                float angleDiff = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);
                turnRatio = Mathf.Clamp(angleDiff / 30f, -1f, 1f);

                float turnSpeed = (currentState == AnimalState.Panic) ? 60f : 45f;
                Quaternion lookRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
            }

            movementXZ = transform.forward * currentSpeed;
            UpdateAnimator(speedRatio, turnRatio, isGrounded, 1f, verticalVelocity);
        }
        else
        {
            UpdateAnimator(speedRatio, 0f, isGrounded, 1f, verticalVelocity);
        }

        Vector3 finalPosition = transform.position + (movementXZ * Time.deltaTime);
        finalPosition.y = nextY;
        transform.position = finalPosition;
        agent.nextPosition = transform.position;
    }

    private void UpdateAnimator(float speed, float turn, bool grounded, float animMultiplier, float vVel)
    {
        animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        animator.SetFloat(turnHash, turn, 0.1f, Time.deltaTime);
        animator.SetBool(groundedHash, grounded);
        animator.SetFloat(animMultiplierHash, animMultiplier);
        animator.SetFloat(verticalVelHash, vVel);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KorkutServerRpc(Vector3 tehlikeKaynagi)
    {
        if (!IsServer || isRidden) return;

        currentState = AnimalState.Panic;
        stateTimer = panicDuration;
        animator.SetBool(eatHash, false);

        Vector3 kacisYonu = (transform.position - tehlikeKaynagi).normalized;
        Vector3 kacisHedefi = transform.position + (kacisYonu * panicEscapeRadius);
        SetRandomDestination(kacisHedefi, panicEscapeRadius / 2f);
    }
}