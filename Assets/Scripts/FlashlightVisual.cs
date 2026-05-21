using UnityEngine;

public class FlashlightVisual : MonoBehaviour
{
    [Header("Bileþenler")]
    public Light spotlight;
    public AudioSource audioSource;
    public AudioClip clickSound;

    private PlayerFlashlightSync syncSystem;
    private bool lastState = false;

    private void Start()
    {
        syncSystem = GetComponentInParent<PlayerFlashlightSync>();

        if (syncSystem != null)
        {
            lastState = syncSystem.isLightOn.Value;
            if (spotlight != null) spotlight.enabled = lastState;
        }
    }

    private void Update()
    {
        if (syncSystem == null) return;

        if (syncSystem.isLightOn.Value != lastState)
        {
            lastState = syncSystem.isLightOn.Value;

            if (spotlight != null) spotlight.enabled = lastState;
            if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
        }
    }

    // --- ÝÞTE SÝHÝRLÝ DOKUNUÞ BURADA ---
    // Envanter sistemi bu feneri elinden yok ettiði an otomatik çalýþýr
    private void OnDestroy()
    {
        // Eðer fenerin sahibi bizsek, karakterdeki þalteri (að deðiþkenini) sýfýrla
        if (syncSystem != null && syncSystem.IsOwner)
        {
            syncSystem.isLightOn.Value = false;
        }
    }
}