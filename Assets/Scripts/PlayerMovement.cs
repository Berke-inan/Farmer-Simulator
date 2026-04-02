using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    public float speed = 5f;
    public float gravity = -9.81f;
    private Vector3 velocity;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
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

        // Sadece sahibi olduğumuz karakterde bu yazı düşmeli
        Debug.Log("Ben bu karakterin sahibiyim ve yürütebilirim!");

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

      
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Debug.Log("Gelen Input: " + moveInput);


        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * speed * Time.deltaTime);


        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}