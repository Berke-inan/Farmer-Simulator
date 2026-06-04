using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class YakitBidonu : MonoBehaviour
{
    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float dolumHizi = 25f;

    private PlayerInventory inventory;
    private float aktarimBirikimi = 0f;
    private float istasyonBirikimi = 0f;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Update()
    {
        if (inventory == null || !inventory.IsOwner) return;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
            {
                // 1. TRAKTÖRÜ DOLDURMA
                TractorFuelSystem traktor = hit.collider.GetComponentInParent<TractorFuelSystem>();
                if (traktor != null)
                {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(60f, 0, 0), Time.deltaTime * 8f);

                    if (inventory.bidonMevcutYakit.Value > 0 && traktor.currentFuel.Value < traktor.maxFuel)
                    {
                        aktarimBirikimi += dolumHizi * Time.deltaTime;

                        if (aktarimBirikimi >= 2.5f)
                        {
                            inventory.BidondanTraktoreServerRpc(traktor.NetworkObjectId, aktarimBirikimi);
                            aktarimBirikimi = 0f;
                        }
                    }
                    return;
                }

                // 2. YENÝ EKLENEN: ÝSTASYONDAN BÝDONU SOL TIKLA DOLDURMA
                YakitIstasyonu istasyon = hit.collider.GetComponentInParent<YakitIstasyonu>();
                if (istasyon != null)
                {
                    transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);

                    if (inventory.bidonMevcutYakit.Value < 25f && istasyon.currentFuel.Value > 0)
                    {
                        istasyonBirikimi += dolumHizi * Time.deltaTime;

                        if (istasyonBirikimi >= 2.5f)
                        {
                            istasyon.BidonuDoldurMiktarliServerRpc(inventory.NetworkObjectId, istasyonBirikimi);
                            istasyonBirikimi = 0f;
                        }
                    }
                    return;
                }
            }
        }
        else
        {
            // Sol týk býrakýldýðýnda eldeki kalan küçük küsurat birikimleri de aða gönderilir
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

            if (istasyonBirikimi > 0f)
            {
                Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
                Ray ray = new Ray(cam.position, cam.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, dolumMesafesi))
                {
                    YakitIstasyonu istasyon = hit.collider.GetComponentInParent<YakitIstasyonu>();
                    if (istasyon != null) istasyon.BidonuDoldurMiktarliServerRpc(inventory.NetworkObjectId, istasyonBirikimi);
                }
                istasyonBirikimi = 0f;
            }

            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime * 8f);
        }
    }
}