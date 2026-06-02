using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class BoruVanasi : NetworkBehaviour, IInteractable
{
    public static BoruVanasi Instance { get; private set; }

    [Header("Vana Görseli ve Ses")]
    public Transform vanaGorseli;
    public AudioClip vanaSesi;

    public NetworkVariable<int> vanaAsamasi = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> anaSuAcikMi = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> acilmaYonundeMi = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool vanaDonuyor = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        vanaAsamasi.OnValueChanged += VanaAsamasiDegisti;
    }

    public override void OnNetworkDespawn()
    {
        vanaAsamasi.OnValueChanged -= VanaAsamasiDegisti;
    }

    public void Interact(NetworkObject interactor)
    {
        if (vanaDonuyor) return;
        VanaCevirServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void VanaCevirServerRpc()
    {
        if (acilmaYonundeMi.Value)
        {
            vanaAsamasi.Value++;
            if (vanaAsamasi.Value >= 3)
            {
                vanaAsamasi.Value = 3;
                anaSuAcikMi.Value = true;
                acilmaYonundeMi.Value = false;
            }
        }
        else
        {
            vanaAsamasi.Value--;
            if (vanaAsamasi.Value <= 0)
            {
                vanaAsamasi.Value = 0;
                anaSuAcikMi.Value = false;
                acilmaYonundeMi.Value = true;
            }
        }
    }

    private void VanaAsamasiDegisti(int eskiAsama, int yeniAsama)
    {
        bool ileriDogru = yeniAsama > eskiAsama;
        StartCoroutine(PruzsuzVanaDondur(ileriDogru));
    }

    // --- YENÝ: SADECE PÜRÜZSÜZ DÖNEN VANA ANÝMASYONU ---
    private IEnumerator PruzsuzVanaDondur(bool ileri)
    {
        vanaDonuyor = true;
        if (vanaSesi != null) AudioSource.PlayClipAtPoint(vanaSesi, transform.position);

        float gecenZaman = 0f;
        float animasyonSuresi = 0.3f;
        float donecekAci = ileri ? 120f : -120f;
        float hiz = donecekAci / animasyonSuresi;

        while (gecenZaman < animasyonSuresi)
        {
            gecenZaman += Time.deltaTime;
            // DEÐÝÞÝKLÝK: (0, 0, hiz) yerine (0, hiz, 0) yaparak Y eksenine aldýk!
            vanaGorseli.Rotate(0, hiz * Time.deltaTime, 0, Space.Self);
            yield return null;
        }

        vanaDonuyor = false;
    }
}