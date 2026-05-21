using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerFlashlightSync : NetworkBehaviour
{
    // Aðdaki herkesin okuyabildiði, sadece senin yazabildiðin fener durumu
    public NetworkVariable<bool> isLightOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Update()
    {
        // Sadece kendi karakterinse tuþlarý dinle
        if (!IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            // Yalnýzca karakterin elinde "FlashlightVisual" kodlu bir obje varsa F tuþu çalýþsýn
            // Bu sayede elinde fener yokken F'ye basarsan hiçbir þey olmaz (bug önleyici)
            if (GetComponentInChildren<FlashlightVisual>() != null)
            {
                isLightOn.Value = !isLightOn.Value;
            }
        }
    }
}