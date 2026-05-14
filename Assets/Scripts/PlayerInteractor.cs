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

        // E tuşu
        inputActions.Player.Interact.started += ctx => HandleInteraction();
        // F tuşu
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction();
        // G tuşu
        inputActions.Player.Drop.started += ctx => DropItem();
        // Sol Tık
        inputActions.Player.Attack.started += ctx => UseHeldItem();

        inputActions.Player.Holster.started += ctx => inventory.ToggleHolster();

        // --- YENİ: Klavye Slot Değiştirme (1-9) ---
        inputActions.Player.Hotbar1.started += ctx => inventory.ChangeHotbarSlot(0);
        inputActions.Player.Hotbar2.started += ctx => inventory.ChangeHotbarSlot(1);
        inputActions.Player.Hotbar3.started += ctx => inventory.ChangeHotbarSlot(2);
        inputActions.Player.Hotbar4.started += ctx => inventory.ChangeHotbarSlot(3);
        inputActions.Player.Hotbar5.started += ctx => inventory.ChangeHotbarSlot(4);
        inputActions.Player.Hotbar6.started += ctx => inventory.ChangeHotbarSlot(5);
        inputActions.Player.Hotbar7.started += ctx => inventory.ChangeHotbarSlot(6);
        inputActions.Player.Hotbar8.started += ctx => inventory.ChangeHotbarSlot(7);
        inputActions.Player.Hotbar9.started += ctx => inventory.ChangeHotbarSlot(8);
        inputActions.Player.Hotbar0.started += ctx => inventory.ChangeHotbarSlot(9);
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // 1. Yerden eşya alma kontrolü
            if (hit.collider.TryGetComponent(out InteractableItem groundItem))
            {
                // Aktif slot alabilir mi kontrol et
                if (inventory.CanPickupToActiveSlot(groundItem.itemID))
                {
                    // Alabiliyorsak normal pickup
                    inventory.RequestPickupServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
                }
                else
                {
                    // Slot doluysa zıplat
                    inventory.RequestBounceServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
                    Debug.Log("Slot dolu, eşya zıplatılıyor!");
                }
                return;
            }

            // 2. Diğer etkileşimler (Laptop, Traktör vb.)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(NetworkObject);
            }
        }
    }
    private void UseHeldItem()
    {
        // Elimizde bir alet görseli var mı?
        if (inventory != null && inventory.eldekiObje != null)
        {
            // Elimizdeki görselin üzerinde Useable scripti var mı?
            if (inventory.eldekiObje.TryGetComponent(out IUseableTool alet))
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
                {
                    // Aleti çalıştır (Çapa, Tohum vs.)
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
            ISecondaryInteractable secondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (secondary != null) secondary.SecondaryInteract(NetworkObject);
        }
    }

    private void DropItem() => inventory?.EldekiniYereAt();

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null) inputActions.Disable();
    }
}