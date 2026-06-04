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

    [Header("Sýnýr (Hortum) Ayarlarý")]
    [Tooltip("Hortumun depodan çýkýþ noktasý. Boþ býrakýlýrsa otomatik olarak istasyonYuvasi temel alýnýr.")]
    public Transform hortumBaslangicNoktasi;
    public float maxUzaklasmaMesafesi = 11f;

    public NetworkVariable<ulong> tutanOyuncuId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PlayerInventory inventory;
    private float aktarimBirikimi = 0f;
    private Transform aktifElTransform;
    private Collider anaCollider;
    private Rigidbody rb;
    private bool lokalTutanBenMiyim = false;
    private bool sonrakiKareBirakabilir = false;

    private Quaternion guncelEgim = Quaternion.identity;

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
                    aktifElTransform = inventory.localHandTransform;
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
            guncelEgim = Quaternion.identity;
            if (rb != null) rb.isKinematic = true;
            if (anaCollider != null) anaCollider.enabled = true;
        }
    }

    private void Update()
    {
        bool traktorDolduruluyorMu = false;

        if (inventory != null && inventory.IsOwner)
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
                Ray ray = new Ray(cam.position, cam.forward);

                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                    if (traktor != null && traktor.currentFuel.Value < traktor.maxFuel)
                    {
                        traktorDolduruluyorMu = true;
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

        if (aktifElTransform != null)
        {
            transform.position = aktifElTransform.position;

            if (traktorDolduruluyorMu)
            {
                guncelEgim = Quaternion.Lerp(guncelEgim, Quaternion.Euler(20f, 0f, 0f), Time.deltaTime * 8f);
            }
            else
            {
                guncelEgim = Quaternion.Lerp(guncelEgim, Quaternion.identity, Time.deltaTime * 8f);
            }

            transform.rotation = aktifElTransform.rotation * guncelEgim;
        }
        else if (tutanOyuncuId.Value == 0 && istasyonYuvasi != null)
        {
            transform.position = istasyonYuvasi.position;
            transform.rotation = istasyonYuvasi.rotation;
        }

        if (lokalTutanBenMiyim)
        {
            // --- HORTUM MESAFE KONTROLÜ (ENTEGRE EDÝLDÝ) ---
            Transform baslangic = hortumBaslangicNoktasi != null ? hortumBaslangicNoktasi : istasyonYuvasi;
            if (baslangic != null)
            {
                float mesafe = Vector3.Distance(transform.position, baslangic.position);
                if (mesafe > maxUzaklasmaMesafesi)
                {
                    Debug.Log("Hortum çok gerildi, pompa elden düþtü!");
                    PompayiBrakServerRpc();
                    return; // Elden düþtüðü için alt satýrlardaki envanter kilidini çalýþtýrma
                }
            }

            if (inventory != null)
            {
                inventory.SetHolstered(true);
                if (inventory.eldekiObje != null && inventory.eldekiObje.activeSelf)
                {
                    inventory.eldekiObje.SetActive(false);
                }
            }

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
    }

    public void Interact(NetworkObject user)
    {
        if (tutanOyuncuId.Value == 0)
        {
            if (user.TryGetComponent(out PlayerInventory inv))
            {
                int activeIdx = inv.activeHotbarIndex.Value;
                if (!inv.slots[activeIdx].IsEmpty) return;
            }
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
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer.TryGetComponent(out PlayerInventory localInv))
                {
                    int activeIdx = localInv.activeHotbarIndex.Value;
                    if (!localInv.slots[activeIdx].IsEmpty)
                    {
                        return new List<ActionPrompt>
                        {
                            new ActionPrompt("ELÝN DOLU!", "Önce elindeki eþyayý býrakmalýsýn")
                        };
                    }
                }
            }

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