using UnityEngine;

public class FlashlightVisual : MonoBehaviour
{
    [Header("Bileþenler")]
    public Light spotlight;
    public Light pointLight; // YENÝ: Ampul parlama efekti için eklenen Point Light
    public AudioSource audioSource;
    public AudioClip clickSound;

    private PlayerFlashlightSync syncSystem;

    private void Start()
    {
        syncSystem = GetComponentInParent<PlayerFlashlightSync>();

        // Envanterden feneri eline her aldýðýnda (Instantiate anýnda)
        // aðda fener açýk mý kapalý mý kontrol et ve durumunu anýnda üzerine uygula
        if (syncSystem != null)
        {
            syncSystem.RefreshFlashlightVisuals(syncSystem.isLightOn.Value);
        }
    }

    private void OnDestroy()
    {
        // Fener envanterde deðiþtirilip yok edildiðinde þalteri güvenle kapatýr
        if (syncSystem != null && syncSystem.IsOwner)
        {
            syncSystem.SetLightStateServerRpc(false);
        }
    }
}