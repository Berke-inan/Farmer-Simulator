using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    public float interactionDistance = 5f;
    public Transform playerCamera;

    private InputSystem_Actions inputActions;
    private PlayerInventory inventory;

    // YENİ: Hangi eşyaya baktığımızı aklında tutar
    private PickupableTool currentHighlightedTool;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inventory = GetComponent<PlayerInventory>();

        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Interact.started += ctx => HandleInteraction();
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction();
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

    // YENİ: Sadece senin oyuncun (IsOwner) etrafı tarar
    private void Update()
    {
        if (!IsOwner) return;
        CheckForHighlights();
    }

    // YENİ: Kameranın baktığı eşyayı parlatma kontrolü
    private void CheckForHighlights()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            PickupableTool tool = hit.collider.GetComponentInParent<PickupableTool>();

            if (tool != null)
            {
                if (tool != currentHighlightedTool)
                {
                    if (currentHighlightedTool != null) currentHighlightedTool.ParlamaKapat();
                    currentHighlightedTool = tool;
                    currentHighlightedTool.ParlamaAc();
                }
                return;
            }
        }

        if (currentHighlightedTool != null)
        {
            currentHighlightedTool.ParlamaKapat();
            currentHighlightedTool = null;
        }
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(NetworkObject);
            }
            else if (inventory != null && inventory.eldekiObje != null)
            {
                if (inventory.eldekiObje.TryGetComponent(out IUseableTool alet))
                {
                    alet.EylemYap(hit, inventory);
                }
            }
        }
    }

    private void HandleSecondaryInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
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