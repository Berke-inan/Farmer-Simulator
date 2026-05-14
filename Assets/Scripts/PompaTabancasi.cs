using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PompaTabancasi : MonoBehaviour
{
    [Header("Baðlantý")]
    public YakitIstasyonu bagliIstasyon;

    [Header("Dolum Ayarlarý")]
    public float dolumMesafesi = 4f;
    public float traktorDolumHizi = 20f;

    private PlayerInventory inventory;
    private float aktarimBirikimi = 0f;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Update()
    {
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
}