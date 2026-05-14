using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class YakitBidonu : MonoBehaviour
{
    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float dolumHizi = 25f; // Saniyede 25L

    private PlayerInventory inventory;
    private float aktarimBirikimi = 0f;

    private void Start()
    {
        // Oyuncuyu bulur
        inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Update()
    {
        if (inventory == null || !inventory.IsOwner) return;

        // R tuþuna basýlý tutuluyorsa
        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
            {
                TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                if (traktor != null)
                {
                    // Animasyon: Bidonu eð
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(60f, 0, 0), Time.deltaTime * 8f);

                    // Bidonda yakýt varsa ve traktör dolmadýysa
                    if (inventory.bidonMevcutYakit.Value > 0 && traktor.currentFuel.Value < traktor.maxFuel)
                    {
                        aktarimBirikimi += dolumHizi * Time.deltaTime;

                        if (aktarimBirikimi >= 2.5f)
                        {
                            inventory.BidondanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                            aktarimBirikimi = 0f;
                        }
                    }
                }
            }
        }
        else
        {
            // R tuþu býrakýldýysa bidonu düzelt
            if (aktarimBirikimi > 0f)
            {
                Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
                Ray ray = new Ray(cam.position, cam.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                    if (traktor != null) inventory.BidondanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                }
                aktarimBirikimi = 0f;
            }

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }
    }
}