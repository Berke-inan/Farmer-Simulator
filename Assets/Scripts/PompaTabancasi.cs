using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PompaTabancasi : NetworkBehaviour, IInteractable
{
    [Header("Baðlantý")]
    public YakitIstasyonu bagliIstasyon;

    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float traktorDolumHizi = 20f;

    [Header("Yuva Ayarlarý")]
    public Transform istasyonYuvasi;

    public NetworkVariable<ulong> tutanOyuncuId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerInventory inventory;
    private float aktarimBirikimi = 0f;
    private Transform aktifElTransform;
    private Collider anaCollider;
    private Rigidbody rb;
    private bool lokalTutanBenMiyim = false;
    private bool sonrakiKareBirakabilir = false;

    private void Awake()
    {
        anaCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        tutanOyuncuId.OnValueChanged += OnTutanOyuncuChanged;
        if (tutanOyuncuId.Value != 0)
        {
            OnTutanOyuncuChanged(0, tutanOyuncuId.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        tutanOyuncuId.OnValueChanged -= OnTutanOyuncuChanged;
    }

    private void OnTutanOyuncuChanged(ulong oldId, ulong newId)
    {
        if (newId != 0)
        {
            sonrakiKareBirakabilir = false;
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newId, out NetworkObject playerObj))
            {
                inventory = playerObj.GetComponent<PlayerInventory>();
                if (inventory != null)
                {
                    aktifElTransform = inventory.handTransform;
                    if (playerObj.IsOwner)
                    {
                        lokalTutanBenMiyim = true;
                        inventory.SetHolstered(true);
                    }
                }
            }
            if (rb != null) rb.isKinematic = true;
            if (anaCollider != null) anaCollider.enabled = false;
        }
        else
        {
            if (inventory != null && inventory.IsOwner)
            {
                inventory.SetHolstered(false);
            }

            inventory = null;
            aktifElTransform = null;
            aktarimBirikimi = 0f;
            lokalTutanBenMiyim = false;
            sonrakiKareBirakabilir = false;
            if (rb != null) rb.isKinematic = true;
            if (anaCollider != null) anaCollider.enabled = true;
        }
    }

    private void Update()
    {
        if (aktifElTransform != null)
        {
            transform.position = aktifElTransform.position;
            transform.rotation = aktifElTransform.rotation;
        }
        else if (tutanOyuncuId.Value == 0 && istasyonYuvasi != null)
        {
            transform.position = istasyonYuvasi.position;
            transform.rotation = istasyonYuvasi.rotation;
        }

        if (lokalTutanBenMiyim)
        {
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                if (sonrakiKareBirakabilir)
                {
                    PompayiBrakServerRpc();
                    return;
                }
            }

            if (!sonrakiKareBirakabilir)
            {
                sonrakiKareBirakabilir = true;
            }
        }

        if (inventory == null || !inventory.IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
            {
                TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                if (traktor != null && traktor.currentFuel.Value < traktor.maxFuel)
                {
                    aktarimBirikimi += traktorDolumHizi * Time.deltaTime;
                    if (aktarimBirikimi >= 2.5f)
                    {
                        inventory.YakitAktarServerRpc(traktor.NetworkObjectId, aktarimBirikimi, bagliIstasyon.NetworkObjectId);
                        aktarimBirikimi = 0f;
                    }
                }
            }
        }
    }

    public void Interact(NetworkObject user)
    {
        if (tutanOyuncuId.Value == 0)
        {
            PompayiAlServerRpc(user.NetworkObjectId);
        }
    }

    [Rpc(SendTo.Server)]
    private void PompayiAlServerRpc(ulong playerId)
    {
        tutanOyuncuId.Value = playerId;
    }

    [Rpc(SendTo.Server)]
    private void PompayiBrakServerRpc()
    {
        tutanOyuncuId.Value = 0;
    }

    public List<ActionPrompt> GetPrompts()
    {
        if (tutanOyuncuId.Value == 0)
        {
            return new List<ActionPrompt>
            {
                new ActionPrompt("E", "Yakýt Pompasýný Al")
            };
        }
        else
        {
            return new List<ActionPrompt>
            {
                new ActionPrompt("G", "Pompayý Yuvasýna Býrak")
            };
        }
    }
}