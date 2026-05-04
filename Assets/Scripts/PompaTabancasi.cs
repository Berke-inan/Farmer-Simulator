using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PickupableTool))]
public class PompaTabancasi : NetworkBehaviour
{
    [Header("Baðlantý")]
    [Tooltip("Bu pompa hangi depodan yakýt çekecek?")]
    public YakitIstasyonu bagliIstasyon;

    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float traktorDolumHizi = 20f; // Saniyede 20L

    private PickupableTool pickupTool;
    private float aktarimBirikimi = 0f;

    private void Awake()
    {
        pickupTool = GetComponent<PickupableTool>();
    }

    private void Update()
    {
        if (!IsOwner || !pickupTool.isEquipped.Value || pickupTool.isStored.Value) return;

        // Pompa elimizde ve R tuþuna BASILI TUTULUYORSA
        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            if (pickupTool.targetCamera != null)
            {
                Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    // SADECE TRAKTÖRÜ DOLDURUR
                    TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                    if (traktor != null && traktor.currentFuel.Value < traktor.maxFuel)
                    {
                        aktarimBirikimi += traktorDolumHizi * Time.deltaTime;
                        if (aktarimBirikimi >= 2.5f)
                        {
                            PompadanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                            aktarimBirikimi = 0f;
                        }
                    }
                }
            }
        }
        else
        {
            // R tuþu BIRAKILDIYSA küsuratý yolla
            if (aktarimBirikimi > 0f)
            {
                if (pickupTool.targetCamera != null)
                {
                    Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                    if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                    {
                        TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                        if (traktor != null) PompadanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                    }
                }
                aktarimBirikimi = 0f;
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void PompadanTraktoreServerRpc(ulong traktorID, float istenenMiktar)
    {
        if (bagliIstasyon == null) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(traktorID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TractorFuelSystem traktor))
            {
                float bosYer = traktor.maxFuel - traktor.currentFuel.Value;
                float eklenecek = Mathf.Min(istenenMiktar, bosYer);

                // Ýstasyondan yakýtý çekmeyi dene 
                float gercektenCekilen = bagliIstasyon.YakitCek(eklenecek);

                if (gercektenCekilen > 0)
                {
                    traktor.AddFuelServerRpc(gercektenCekilen);
                }
            }
        }
    }
}