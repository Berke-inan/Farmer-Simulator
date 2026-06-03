using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    public float interactionDistance = 2.5f;
    public Transform playerCamera;

    private InputSystem_Actions inputActions;
    private PlayerInventory inventory;
    private OutlineGlow currentGlowingObject;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inventory = GetComponent<PlayerInventory>();
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Interact.started += ctx => HandleInteraction();
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction();
        inputActions.Player.Drop.started += ctx => DropItem();
        inputActions.Player.Attack.started += ctx => UseHeldItem();
        inputActions.Player.Holster.started += ctx => inventory.ToggleHolster();

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

    private void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        float scrollY = Mouse.current.scroll.ReadValue().y;
        if (scrollY != 0)
        {
            int currentIndex = inventory.activeHotbarIndex.Value;

            if (scrollY > 0)
            {
                currentIndex--;
                if (currentIndex < 0) currentIndex = 9;
            }
            else
            {
                currentIndex++;
                if (currentIndex > 9) currentIndex = 0;
            }

            inventory.ChangeHotbarSlot(currentIndex);
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        // Ekrana basılacak tuşları tutacağımız geçici liste
        List<ActionPrompt> currentPrompts = new List<ActionPrompt>();

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            OutlineGlow targetGlow = hit.collider.GetComponentInParent<OutlineGlow>();

            if (targetGlow != null)
            {
                if (currentGlowingObject != targetGlow)
                {
                    if (currentGlowingObject != null) currentGlowingObject.DisableGlow();
                    currentGlowingObject = targetGlow;
                    currentGlowingObject.EnableGlow();
                }
            }
            else if (currentGlowingObject != null)
            {
                currentGlowingObject.DisableGlow();
                currentGlowingObject = null;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                currentPrompts.AddRange(interactable.GetPrompts());
            }

            ISecondaryInteractable secondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (secondary != null)
            {
                // currentPrompts.AddRange(secondary.GetPrompts()); 
            }
        }
        else
        {
            if (currentGlowingObject != null)
            {
                currentGlowingObject.DisableGlow();
                currentGlowingObject = null;
            }
        }

        if (currentPrompts.Count == 0 && inventory != null && inventory.eldekiObje != null)
        {
            if (inventory.eldekiObje.TryGetComponent(out IUseableTool alet))
            {
                currentPrompts.Add(new ActionPrompt("Sol Tık", "KULLAN"));
            }

            currentPrompts.Add(new ActionPrompt("G", "YERE AT"));
        }

        // ÇÖZÜM BURASI: Bu satırı yorum satırından çıkardım, artık yazılar UI'a iletilecek!
        if (FarmerSimulator.UI.HUDManager.Instance != null)
        {
            FarmerSimulator.UI.HUDManager.Instance.UpdateActionPrompts(currentPrompts);
        }
    }

    private void HandleInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent(out InteractableItem groundItem))
            {
                if (inventory.CanPickupToActiveSlot(groundItem.itemID))
                {
                    inventory.RequestPickupServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
                }
                else
                {
                    inventory.RequestBounceServerRpc(hit.collider.GetComponent<NetworkObject>().NetworkObjectId);
                }
                return;
            }

            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null) interactable.Interact(NetworkObject);
        }
    }

    private void UseHeldItem()
    {
        if (inventory != null && inventory.eldekiObje != null)
        {
            if (inventory.eldekiObje.TryGetComponent(out IUseableTool alet))
            {
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
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