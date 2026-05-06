using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class YakitIstasyonu : NetworkBehaviour
{
    [Header("Ýstasyon Ayarlarý")]
    public NetworkVariable<float> istasyonYakiti = new NetworkVariable<float>(1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Bidon Dolum Ayarlarý")]
    public float algilamaMesafesi = 6f;
    public float bidonDolumHizi = 25f; // Saniyede 25L

    private float istasyonAktarimBirikimi = 0f;

    // ==========================================
    // POMPANIN KULLANACAÐI "YAKIT ÇEKME" SÝSTEMÝ
    // ==========================================
    public float YakitCek(float istenenMiktar)
    {
        if (istasyonYakiti.Value <= 0) return 0f;

        float verilecek = Mathf.Min(istenenMiktar, istasyonYakiti.Value);
        istasyonYakiti.Value -= verilecek;

        return verilecek;
    }

    // ==========================================
    // BÝDONU ESKÝSÝ GÝBÝ ÝSTASYONDAN DOLDURMA SÝSTEMÝ
    // ==========================================
    private void Update()
    {
        if (!IsSpawned || NetworkManager.Singleton == null || !IsClient) return;

        // R tuþuna BASILI TUTULUYORSA
        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient == null || localClient.PlayerObject == null) return;

            PlayerInteractor pi = localClient.PlayerObject.GetComponent<PlayerInteractor>();
            if (pi == null || pi.playerCamera == null) return;

            if (Vector3.Distance(transform.position, pi.transform.position) > algilamaMesafesi) return;

            Ray ray = new Ray(pi.playerCamera.position, pi.playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, algilamaMesafesi))
            {
                if (hit.collider.GetComponentInParent<YakitIstasyonu>() == this)
                {
                    PlayerInventory inventory = pi.GetComponent<PlayerInventory>();

                    // ELÝNDE BÝDON VARSA (Ýstasyon sadece bidon doldurur, traktör aramaz)
                    if (inventory != null && inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out YakitBidonu bidon))
                    {
                        if (bidon.mevcutYakit.Value < bidon.maxKapasite && istasyonYakiti.Value > 0)
                        {
                            istasyonAktarimBirikimi += bidonDolumHizi * Time.deltaTime;
                            if (istasyonAktarimBirikimi >= 2.5f)
                            {
                                IstasyondanBidonaServerRpc(bidon.NetworkObjectId, istasyonAktarimBirikimi);
                                istasyonAktarimBirikimi = 0f;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            istasyonAktarimBirikimi = 0f;
        }
    }

    [Rpc(SendTo.Server)]
    private void IstasyondanBidonaServerRpc(ulong bidonID, float miktar)
    {
        if (istasyonYakiti.Value < miktar) miktar = istasyonYakiti.Value;
        if (miktar <= 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bidonID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out YakitBidonu bidon))
            {
                float bosYer = bidon.maxKapasite - bidon.mevcutYakit.Value;
                float eklenecek = Mathf.Min(miktar, bosYer);

                if (eklenecek > 0)
                {
                    bidon.mevcutYakit.Value += eklenecek;
                    istasyonYakiti.Value -= eklenecek;
                }
            }
        }
    }
}