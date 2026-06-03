using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class Switch : NetworkBehaviour, IInteractable
{
    [Header("Iþýk Ayarlarý")]
    public Light[] lights;
    private bool isOn;

    [Header("Ses Ayarlarý")]
    [Tooltip("Anahtarýn üzerindeki Audio Source bileþeni")]
    public AudioSource audioSource;
    [Tooltip("Çalýnacak Çýt (Click) sesi")]
    public AudioClip clickSound;

    // Þaltere bakýldýðýnda HUD'da görünecek yönerge
    public List<ActionPrompt> GetPrompts()
    {
        string eylemMetni = isOn ? "SÖNDÜR" : "YAK";

        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", eylemMetni)
        };
    }

    public void Interact(NetworkObject interactor)
    {
        // 1. Iþýklarýn durumunu tersine çevir
        isOn = !isOn;
        foreach (Light l in lights)
        {
            l.enabled = isOn;
        }

        // 2. Ses kaynaðý ve ses dosyasý atanmýþsa sesi çal
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log("Switch çalýþtý: Iþýklar " + (isOn ? "Açýldý" : "Kapandý"));
    }
}