using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    public float interactionDistance = 5f;
    public Transform playerCamera;

    private InputSystem_Actions inputActions;
    private PlayerInventory inventory;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inventory = GetComponent<PlayerInventory>();

        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        // E tuşu (Normal Etkileşim - Alma vb.)
        inputActions.Player.Interact.started += ctx => HandleInteraction();

        // F tuşu (İkincil Etkileşim - Balyalama vb.)
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction();

        // G tuşu (Yere Atma)
        inputActions.Player.Drop.started += ctx => DropItem();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Interact.started -= ctx => HandleInteraction();
            inputActions.Player.SecondaryInteract.started -= ctx => HandleSecondaryInteraction();
            inputActions.Player.Drop.started -= ctx => DropItem();
            inputActions.Disable();
        }
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // TryGetComponent yerine GetComponentInParent kullanıyoruz. 
            // Böylece ışın kasanın zeminine bile çarpsa, ana objedeki Romork scriptini bulur.
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(NetworkObject);
            }
        }
    }

    private void HandleSecondaryInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Aynı şekilde F tuşu için de Parent (Ebeveyn) kontrolü ekliyoruz.
            ISecondaryInteractable secondaryInteractable = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (secondaryInteractable != null)
            {
                secondaryInteractable.SecondaryInteract(NetworkObject);
            }
        }
    }

    private void DropItem()
    {
        if (inventory != null)
        {
            inventory.EldekiniYereAt();
        }
    }
}