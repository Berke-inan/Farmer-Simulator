using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Switch : NetworkBehaviour, IInteractable
{
    [Header("Iþýk Ayarlarý")]
    public Light[] lights;

    // --- DEÐÝÞTÝRÝLEN KISIM 1: AÐ DEÐÝÞKENÝ ---
    // Iþýðýn durumunu aðdaki herkesin senkronize görmesini saðlayan þalter deðiþkeni
    public NetworkVariable<bool> isOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Ses Ayarlarý")]
    [Tooltip("Anahtarýn üzerindeki Audio Source bileþeni")]
    public AudioSource audioSource;
    [Tooltip("Çalýnacak Çýt (Click) sesi")]
    public AudioClip clickSound;

    public override void OnNetworkSpawn()
    {
        // Að deðiþkeni her deðiþtiðinde otomatik tetiklenecek callback fonksiyonunu baðlýyoruz
        isOn.OnValueChanged += OnSwitchStateChanged;

        // Oyuna sonradan giren oyuncular için ýþýklarýn mevcut durumunu hemen senkronize et
        ApplyLightState(isOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        isOn.OnValueChanged -= OnSwitchStateChanged;
    }

    // --- DEÐÝÞTÝRÝLEN KISIM 2: ONDATACHANGED KÖPRÜSÜ ---
    // Bu fonksiyon Server þalteri deðiþtirdiði an TÜM oyuncularýn bilgisayarýnda ayný karede çalýþýr
    private void OnSwitchStateChanged(bool oldState, bool newState)
    {
        ApplyLightState(newState);

        // Ses kaynaðýnýn Spatial Blend ayarý 1 (3D) ise, çýt sesini þalterin yakýnýndaki herkes duyar
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log($"[Að] Switch durumu güncellendi: Iþýklar " + (newState ? "Açýldý" : "Kapandý"));
    }

    private void ApplyLightState(bool state)
    {
        if (lights == null) return;
        foreach (Light l in lights)
        {
            if (l != null) l.enabled = state;
        }
    }

    // Þaltere bakýldýðýnda HUD'da görünecek yönerge
    public List<ActionPrompt> GetPrompts()
    {
        // NetworkVariable olduðu için sonuna .Value ekliyoruz
        string eylemMetni = isOn.Value ? "SÖNDÜR" : "YAK";

        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", eylemMetni)
        };
    }

    public void Interact(NetworkObject interactor)
    {
        // Ýstemciler (Client) að deðiþkenine doðrudan yazamaz. Bu yüzden ServerRpc tetikliyoruz.
        ToggleSwitchServerRpc();
    }

    // --- YENÝ EKLENEN: SERVER RPC GÜVENLÝK DUVARI ---
    [Rpc(SendTo.Server)]
    private void ToggleSwitchServerRpc()
    {
        // Sunucu doðrulamayý yapar ve þalter deðerini tersine çevirir
        isOn.Value = !isOn.Value;
    }
}