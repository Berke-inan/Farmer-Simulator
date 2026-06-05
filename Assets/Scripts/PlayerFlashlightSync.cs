using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFlashlightSync : NetworkBehaviour
{
    // Co-op uyumlu güvenli að þalteri
    public NetworkVariable<bool> isLightOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Þalter her deðiþtiðinde otomatik olarak bu fonksiyon tüm client'larda çalýþýr
        isLightOn.OnValueChanged += OnFlashlightStateChanged;

        // Oyuna sonradan giren biri olursa fenerin durumunu hemen eþitle
        RefreshFlashlightVisuals(isLightOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        isLightOn.OnValueChanged -= OnFlashlightStateChanged;
    }

    private void Update()
    {
        // Tuþ kontrolünü sadece local oyuncu yapar
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            // Bug önleyici: Elimizde fener görseli varsa server'a istek gönder
            if (GetComponentInChildren<FlashlightVisual>(true) != null)
            {
                ToggleFlashlightServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void ToggleFlashlightServerRpc()
    {
        isLightOn.Value = !isLightOn.Value;
    }

    [Rpc(SendTo.Server)]
    public void SetLightStateServerRpc(bool state)
    {
        isLightOn.Value = state;
    }

    private void OnFlashlightStateChanged(bool oldState, bool newState)
    {
        RefreshFlashlightVisuals(newState);
    }

    // --- TÜM CÝHAZLARDA GÖRSELÝ VE SESÝ YENÝLEYEN ANA GÖVDE ---
    public void RefreshFlashlightVisuals(bool state)
    {
        // 'true' parametresi sayesinde hiyerarþide gizlenmiþ/inaktif olan modelleri de tarar
        FlashlightVisual visual = GetComponentInChildren<FlashlightVisual>(true);

        if (visual != null)
        {
            if (visual.spotlight != null) visual.spotlight.enabled = state;

            // Klik sesini fenerin olduðu konumdan tüm co-op lobisine 3D yayýnlar
            if (visual.audioSource != null && visual.clickSound != null)
            {
                visual.audioSource.PlayOneShot(visual.clickSound);
            }
        }
    }
}