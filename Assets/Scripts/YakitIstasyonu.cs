using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class YakitIstasyonu : NetworkBehaviour
{
    [Header("Ýstasyon Ayarlarý")]
    public NetworkVariable<float> istasyonYakiti = new NetworkVariable<float>(1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Tooltip("Traktörü istasyonun ne kadar yakýnýna park etmek gerekiyor?")]
    public float traktorAlgilamaMesafesi = 6f;

    [Header("Hýz Ayarlarý")]
    [Tooltip("Bidonun saniyede dolma hýzý (25L'yi 1 saniyede doldurur)")]
    public float bidonDolumHizi = 25f;

    [Tooltip("Traktörün saniyede dolma hýzý (100L'yi 5 saniyede doldurur)")]
    public float traktorDolumHizi = 20f;

    private float istasyonAktarimBirikimi = 0f;

    private void Update()
    {
        // 1. KESÝN GÜVENLÝK DUVARI: Obje aðda spawn olmadýysa veya NetworkManager henüz yoksa HÝÇ BAÞLAMA!
        if (!IsSpawned || NetworkManager.Singleton == null || !IsClient) return;

        // Eðer oyuncu R tuþuna BASILI TUTUYORSA
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.rKey.isPressed)
        {
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient == null || localClient.PlayerObject == null) return;

            PlayerInteractor pi = localClient.PlayerObject.GetComponent<PlayerInteractor>();
            if (pi == null || pi.playerCamera == null) return;

            // Ýstasyona yeterince yakýn mýyýz?
            if (Vector3.Distance(transform.position, pi.transform.position) > traktorAlgilamaMesafesi) return;

            // Kamerayla istasyona bakýyor muyuz?
            Ray ray = new Ray(pi.playerCamera.position, pi.playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, traktorAlgilamaMesafesi))
            {
                if (hit.collider.GetComponentInParent<YakitIstasyonu>() == this)
                {
                    PlayerInventory inventory = pi.GetComponent<PlayerInventory>();

                    // 1. DURUM: ELÝNDE BÝDON VARSA (1 Saniyede Dolar)
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
                        return; // Bidonu dolduruyorsan traktör iþlemine geçme
                    }

                    // 2. DURUM: ELÝ BOÞ, YAKINDA TRAKTÖR VARSA (5 Saniyede Dolar)
                    Collider[] hitColliders = Physics.OverlapSphere(transform.position, traktorAlgilamaMesafesi);
                    foreach (var col in hitColliders)
                    {
                        TractorFuelSystem traktor = col.GetComponentInParent<TractorFuelSystem>();
                        if (traktor != null && traktor.currentFuel.Value < traktor.maxFuel && istasyonYakiti.Value > 0)
                        {
                            istasyonAktarimBirikimi += traktorDolumHizi * Time.deltaTime;
                            if (istasyonAktarimBirikimi >= 2.5f)
                            {
                                IstasyondanTraktoreServerRpc(traktor.NetworkObjectId, istasyonAktarimBirikimi);
                                istasyonAktarimBirikimi = 0f;
                            }
                            return; // Ýlk bulduðun traktörü doldur ve çýk
                        }
                    }
                }
            }
        }
        else
        {
            // Tuþ býrakýldýðýnda birikeni sýfýrla
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

    [Rpc(SendTo.Server)]
    private void IstasyondanTraktoreServerRpc(ulong traktorID, float miktar)
    {
        if (istasyonYakiti.Value < miktar) miktar = istasyonYakiti.Value;
        if (miktar <= 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TractorFuelSystem traktor))
            {
                float bosYer = traktor.maxFuel - traktor.currentFuel.Value;
                float eklenecek = Mathf.Min(miktar, bosYer);

                if (eklenecek > 0)
                {
                    traktor.AddFuelServerRpc(eklenecek);
                    istasyonYakiti.Value -= eklenecek;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traktorAlgilamaMesafesi);
    }
}