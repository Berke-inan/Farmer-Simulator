using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Interaction Settings")]
    public float interactionDistance = 5f;
    public Transform playerCamera;

    private InputSystem_Actions inputActions;
    private InventoryManager inventory;
    private NetworkedHotbar hotbar;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Referansları al
        inventory = GetComponent<InventoryManager>();
        hotbar = GetComponent<NetworkedHotbar>();

        // Input sistemini başlat
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        // Tuş atamaları
        inputActions.Player.Interact.started += ctx => HandleInteraction();           // E Tuşu
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction(); // F Tuşu
        inputActions.Player.Drop.started += ctx => DropItem();                        // G Tuşu
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
            int currentSlot = hotbar.ActiveSlotIndex.Value;
            bool elDolu = !inventory.Slots[currentSlot].IsEmpty;

            // 1. ÖNCELİK: Genel Etkileşim (Kapı, Araç, Römork vb.)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null && !(interactable is WorldItem))
            {
                Debug.Log("<color=green>[Interactor]</color> Obje ile etkileşime giriliyor.");
                interactable.Interact(NetworkObject);
                return;
            }

            // 2. ÖNCELİK: El doluysa aleti (Çapa, Orak, Gübre) kullanmayı dene
            if (elDolu && hotbar.CurrentEquippedObject != null)
            {
                if (hotbar.CurrentEquippedObject.TryGetComponent(out IUseableTool alet))
                {
                    Debug.Log("<color=blue>[Interactor]</color> Alet kullanılıyor.");
                    alet.EylemYap(hit, inventory);
                    return;
                }
            }

            // 3. ÖNCELİK: Yerden eşya al (WorldItem)
            if (hit.collider.TryGetComponent(out NetworkObject netObj) && netObj.TryGetComponent(out WorldItem _))
            {
                Debug.Log("<color=white>[Interactor]</color> Eşya toplama isteği gönderildi.");
                TryPickupItemServerRpc(netObj.NetworkObjectId, currentSlot);
            }
        }
    }

    private void HandleSecondaryInteraction()
    {
        // F Tuşu - İkincil etkileşimler (Örn: Römork kapağını açma/kapama)
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            ISecondaryInteractable secondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (secondary != null)
            {
                Debug.Log("<color=yellow>[Interactor]</color> İkincil etkileşim çalıştı.");
                secondary.SecondaryInteract(NetworkObject);
            }
        }
    }

    private void DropItem()
    {
        if (inventory != null && hotbar != null)
        {
            int currentSlot = hotbar.ActiveSlotIndex.Value;
            if (!inventory.Slots[currentSlot].IsEmpty)
            {
                // Kameranın pozisyonunu ve baktığı yönü hesapla
                Vector3 camPos = playerCamera.position + (playerCamera.forward * 0.5f); // Biraz önünde doğsun
                Vector3 camDir = playerCamera.forward;

                DropItemServerRpc(currentSlot, camPos, camDir);
            }
        }
    }

    // --- SERVER RPC METOTLARI ---

    [ServerRpc]
    private void TryPickupItemServerRpc(ulong targetObjectId, int activeSlot)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent(out WorldItem worldItem))
            {
                // Mesafe kontrolü (Hile engelleme)
                float distance = Vector3.Distance(transform.position, targetObject.transform.position);
                if (distance > interactionDistance * 1.5f) return;

                if (inventory.HasSpaceFor(worldItem.ItemData, worldItem.Amount))
                {
                    // Envantere ekle
                    inventory.AddItemServer(worldItem.ItemData, worldItem.Amount, activeSlot);

                    // Objeyi ağdan sil (Despawn uyarısını önlemek için 'true' parametresi)
                    targetObject.Despawn(true);
                }
            }
        }
    }

    [ServerRpc]
    private void DropItemServerRpc(int slotIndex, Vector3 spawnPos, Vector3 throwDir)
    {
        // inventoryManager'a artık bu verileri de yolluyoruz
        inventory.RemoveItemServerRpc(slotIndex, 1, spawnPos, throwDir);
    }
}