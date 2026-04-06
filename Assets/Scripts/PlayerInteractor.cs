using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    public float interactionDistance = 3f;
    private TractorController currentTractor;
    private PlayerMovement playerMovement;
    private InputSystem_Actions inputActions;

    private void Awake()
    {
        // Hareket scriptini referans alıyoruz ki traktöre bindiğimizde kapatabilelim
        playerMovement = GetComponent<PlayerMovement>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Enable();

            // Etkileşim tuşuna (örneğin F) basıldığında sadece bir kez tetiklenir
            inputActions.Player.Interact.started += ctx => HandleInteraction();
        }
    }

    public override void OnNetworkDespawn()
    {
        // Bellek sızıntısını önlemek için event aboneliklerini temizle
        if (IsOwner && inputActions != null)
        {
            inputActions.Player.Interact.started -= ctx => HandleInteraction();
            inputActions.Disable();
        }
    }

    // Update metoduna artık gerek kalmadı! Input sistemi event bazlı çalışıyor.

    private void HandleInteraction()
    {
        if (currentTractor == null || !currentTractor.IsOccupied)
            CheckAndMountTractor();
        else
            DismountTractor();
    }

    private void CheckAndMountTractor()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionDistance);
        foreach (var hit in hitColliders)
        {
            if (hit.TryGetComponent(out TractorController tractor))
            {
                currentTractor = tractor;
                tractor.MountTractor(GetComponent<NetworkObject>());

                // Traktöre binince oyuncu hareketini tamamen devre dışı bırak
                if (playerMovement != null) playerMovement.enabled = false;
                break;
            }
        }
    }

    private void DismountTractor()
    {
        if (currentTractor != null)
        {
            currentTractor.DismountTractor();
            currentTractor = null;

            // Traktörden inince oyuncu hareketini tekrar aktifleştir
            if (playerMovement != null) playerMovement.enabled = true;
        }
    }
}