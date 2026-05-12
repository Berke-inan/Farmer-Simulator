using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    [Header("Ayarlar")]
    public float interactionDistance = 5f;
    public Transform playerCamera;

    private InputSystem_Actions inputActions;
    private InventoryManager inventory;
    private NetworkedHotbar hotbar;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inventory = GetComponent<InventoryManager>();
        hotbar = GetComponent<NetworkedHotbar>();

        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        inputActions.Player.Interact.started += ctx => HandleInteraction();
        inputActions.Player.Attack.started += ctx => HandleUse();
        inputActions.Player.SecondaryInteract.started += ctx => HandleSecondaryInteraction();
        inputActions.Player.Drop.started += ctx => DropItem();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Interact.started -= ctx => HandleInteraction();
            inputActions.Player.Attack.started -= ctx => HandleUse();
            inputActions.Player.SecondaryInteract.started -= ctx => HandleSecondaryInteraction();
            inputActions.Player.Drop.started -= ctx => DropItem();
            inputActions.Disable();
        }
    }

    private void HandleInteraction()
    {
        // ~0 ekleyerek tüm layer'ları görmesini sağlıyoruz (Terrain, Default, vb.)
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, ~0))
        {
            // IInteractable'ı ana objede veya alt objelerde ara
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                if (interactable is WorldItem worldItem)
                {
                    // HATA BURADAYDI: NetworkObject'i sadece çarptığın yerde değil, en üstte ara!
                    NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();

                    if (netObj != null)
                    {
                        Debug.Log($"<color=green>[Alındı]</color> {worldItem.ItemData.ItemName} toplanıyor.");
                        TryPickupItemServerRpc(netObj.NetworkObjectId, hotbar.ActiveSlotIndex.Value);
                    }
                }
                else
                {
                    // Traktör vb. için normal etkileşim
                    interactable.Interact(NetworkObject);
                }
            }
        }
    }

    private void HandleUse()
    {
        int currentSlot = hotbar.ActiveSlotIndex.Value;
        if (!inventory.Slots[currentSlot].IsEmpty && hotbar.CurrentEquippedObject != null)
        {
            if (hotbar.CurrentEquippedObject.TryGetComponent(out IUseableTool alet))
            {
                // Raycast vurduğumuz yer alet kullanımı için (Terrain/Ekin)
                Ray ray = new Ray(playerCamera.position, playerCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, ~0))
                {
                    alet.EylemYap(hit, inventory);
                }
            }
        }
    }

    // --- RPC VE DİĞERLERİ DEĞİŞMEDİ ---

    private void HandleSecondaryInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, ~0))
        {
            ISecondaryInteractable secondary = hit.collider.GetComponentInParent<ISecondaryInteractable>();
            if (secondary != null) secondary.SecondaryInteract(NetworkObject);
        }
    }

    private void DropItem()
    {
        int currentSlot = hotbar.ActiveSlotIndex.Value;
        if (inventory != null && !inventory.Slots[currentSlot].IsEmpty)
        {
            Vector3 camPos = playerCamera.position + (playerCamera.forward * 0.5f);
            Vector3 camDir = playerCamera.forward;
            DropItemServerRpc(currentSlot, camPos, camDir);
        }
    }

    [ServerRpc]
    private void TryPickupItemServerRpc(ulong targetObjectId, int activeSlot)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject targetObject))
        {
            if (targetObject.TryGetComponent(out WorldItem worldItem))
            {
                // 1. ÖNCELİKLİ KONTROL: Seçili (aktif) slot boş mu?
                // Kullanıcının isteği: "Elimdeki slot doluysa alamasın"
                bool slotBosMu = inventory.Slots[activeSlot].IsEmpty;

                if (slotBosMu)
                {
                    // Envanterde yer varsa al (AddItemServer zaten aktif slotu öncelikli tutuyor)
                    if (inventory.HasSpaceFor(worldItem.ItemData, worldItem.Amount))
                    {
                        Debug.Log($"<color=green>[Pickup]</color> {worldItem.ItemData.ItemName} alındı.");
                        inventory.AddItemServer(worldItem.ItemData, worldItem.Amount, activeSlot);
                        targetObject.Despawn(true);
                    }
                }
                else
                {
                    // ELİ DOLUYSA: Eşyayı alma, sadece havaya zıplat!
                    Debug.Log("<color=yellow>[Pickup]</color> El dolu! Eşya havaya fırlatılıyor.");

                    if (targetObject.TryGetComponent(out Rigidbody rb))
                    {
                        // Hafif yukarı ve rastgele yana doğru bir güç verelim
                        Vector3 jumpForce = Vector3.up * 4f + Random.insideUnitSphere * 1f;
                        rb.AddForce(jumpForce, ForceMode.Impulse);

                        // Rastgele bir takla (tork) ekle
                        rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
                    }
                }
            }
        }
    }

    [ServerRpc]
    private void DropItemServerRpc(int slotIndex, Vector3 spawnPos, Vector3 throwDir)
    {
        inventory.RemoveItemServerRpc(slotIndex, 1, spawnPos, throwDir, true);
    }


   

    // Metot olarak tanımla:
    private void ToggleHandVisibility()
    {
        if (hotbar != null)
        {
            // Mevcut durumun tersini gönder (Toggle)
            bool currentStatus = hotbar.isHandVisualHidden.Value;
            hotbar.SetHandVisualVisibilityServerRpc(!currentStatus);

            Debug.Log($"<color=orange>[Hotbar]</color> El Görünürlüğü: {(!currentStatus ? "Gizli" : "Görünür")}");
        }
    }
}