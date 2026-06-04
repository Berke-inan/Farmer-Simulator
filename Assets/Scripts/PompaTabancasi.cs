using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PompaTabancasi : NetworkBehaviour, IInteractable
{
    [Header("Ýstasyon Baðlantýlarý")]
    public YakitIstasyonu bagliIstasyon;
    public Transform istasyonYuvasi;

    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float traktorDolumHizi = 20f;

    public NetworkVariable<ulong> tutanOyuncuId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerInventory cachedInventory;
    private Transform cachedPlayerCamera;
    private Transform aktifElTransform;
    private Rigidbody rb;
    private float aktarimBirikimi = 0f;
    private bool lokalTutanBenMiyim = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (aktifElTransform != null)
        {
            if (rb != null) rb.isKinematic = true;

            transform.position = aktifElTransform.position;
            transform.rotation = aktifElTransform.rotation;

            if (lokalTutanBenMiyim && cachedInventory != null && cachedPlayerCamera != null)
            {
                if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
                {
                    Ray ray = new Ray(cachedPlayerCamera.position, cachedPlayerCamera.forward);

                    if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                    {
                        TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                        if (traktor != null && traktor.currentFuel.Value < traktor.maxFuel)
                        {
                            aktarimBirikimi += traktorDolumHizi * Time.deltaTime;
                            if (aktarimBirikimi >= 2.5f)
                            {
                                cachedInventory.YakitAktarServerRpc(traktor.NetworkObjectId, aktarimBirikimi, bagliIstasyon.NetworkObjectId);
                                aktarimBirikimi = 0f;
                            }
                        }
                    }
                }
            }
        }
        else if (tutanOyuncuId.Value == 0)
        {
            if (rb != null) rb.isKinematic = true;

            if (istasyonYuvasi != null)
            {
                transform.position = istasyonYuvasi.position;
                transform.rotation = istasyonYuvasi.rotation;
            }
        }
    }

    public void Interact(NetworkObject user)
    {
        if (tutanOyuncuId.Value == 0)
        {
            PompayiAlServerRpc(user.NetworkObjectId);
        }
        else if (tutanOyuncuId.Value == user.NetworkObjectId)
        {
            PompayiBrakServerRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void PompayiAlServerRpc(ulong playerId)
    {
        tutanOyuncuId.Value = playerId;
        PompayiAlClientRpc(playerId);
    }

    [Rpc(SendTo.Everyone)]
    private void PompayiAlClientRpc(ulong playerId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();
            PlayerInteractor interactor = playerObj.GetComponent<PlayerInteractor>();

            if (inventory != null)
            {
                cachedInventory = inventory;
                aktifElTransform = inventory.localHandTransform;

                if (interactor != null)
                {
                    cachedPlayerCamera = interactor.playerCamera;
                }

                if (playerObj.IsOwner)
                {
                    lokalTutanBenMiyim = true;
                    inventory.SetHolstered(true);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void PompayiBrakServerRpc()
    {
        tutanOyuncuId.Value = 0;
        PompayiBrakClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PompayiBrakClientRpc()
    {
        if (lokalTutanBenMiyim)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
                inventory.SetHolstered(false);
            }
        }

        lokalTutanBenMiyim = false;
        aktifElTransform = null;
        cachedInventory = null;
        cachedPlayerCamera = null;
        aktarimBirikimi = 0f;
    }

    public List<ActionPrompt> GetPrompts()
    {
        string eylemMetni = tutanOyuncuId.Value == 0 ? "Yakýt Pompasýný Al" : "Pompayý Yuvasýna Býrak";
        return new List<ActionPrompt>
        {
            new ActionPrompt("E", eylemMetni)
        };
    }
}