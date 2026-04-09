using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    public float interactionDistance = 3f;
    public Transform playerCamera; // FPS kameranı buraya sürükle

    private InputSystem_Actions inputActions;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        inputActions.Player.Interact.started += ctx => HandleInteraction();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Interact.started -= ctx => HandleInteraction();
            inputActions.Disable();
        }
    }

    private void HandleInteraction()
    {
        // Kameranın merkezinden ileriye doğru bir ışın (Ray) yolluyoruz
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Baktığımız objede IInteractable var mı kontrol et
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                interactable.Interact(NetworkObject); // Varsa etkileşimi tetikle
            }
        }
    }
}