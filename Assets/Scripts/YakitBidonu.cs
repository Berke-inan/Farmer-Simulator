using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PickupableTool))]
public class YakitBidonu : NetworkBehaviour
{
    [Header("Bidon Ayarlarý")]
    public float maxKapasite = 25f;
    public NetworkVariable<float> mevcutYakit = new NetworkVariable<float>(25f);

    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float dolumHizi = 25f; // Saniyede 25L (25L kapasiteyi tam 1 saniyede boþaltýr)

    private PickupableTool pickupTool;
    private Quaternion orijinalRotasyon;
    private float aktarimBirikimi = 0f;

    private void Awake()
    {
        pickupTool = GetComponent<PickupableTool>();
    }

    private void Update()
    {
        if (!IsOwner || !pickupTool.isEquipped.Value || pickupTool.isStored.Value) return;

        // Eðer R tuþuna BASILI TUTULUYORSA
        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            if (pickupTool.targetCamera != null)
            {
                Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                    if (traktor != null)
                    {
                        // Animasyon: R'ye basýldýðý sürece bidonu yavaþça eð
                        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(60f, 0, 0), Time.deltaTime * 8f);

                        // Yakýt bitmediyse ve traktör dolmadýysa aktarýma baþla
                        if (mevcutYakit.Value > 0 && traktor.currentFuel.Value < traktor.maxFuel)
                        {
                            aktarimBirikimi += dolumHizi * Time.deltaTime;

                            // Her 2.5 Litre biriktiðinde sunucuya yolla (Að optimizasyonu)
                            if (aktarimBirikimi >= 2.5f)
                            {
                                BidondanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                                aktarimBirikimi = 0f;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            // R tuþu BIRAKILDIYSA (Veya traktöre bakýlmýyorsa)
            // Kalan küsurat yakýtý yolla ve bidonu yavaþça düzelt
            if (aktarimBirikimi > 0f)
            {
                Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                    if (traktor != null) BidondanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                }
                aktarimBirikimi = 0f;
            }

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }
    }

    [Rpc(SendTo.Server)]
    private void BidondanTraktoreServerRpc(ulong traktorID, float miktar)
    {
        // Elimizde o kadar yakýt var mý kontrol et
        if (mevcutYakit.Value < miktar) miktar = mevcutYakit.Value;
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
                    mevcutYakit.Value -= eklenecek;
                }
            }
        }
    }
}