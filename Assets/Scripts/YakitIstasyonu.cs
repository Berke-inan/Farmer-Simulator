using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class YakitIstasyonu : NetworkBehaviour, IInteractable
{
    [Header("Depo Kapasite Ayarlarý")]
    public float maxFuel = 1000f;
    public NetworkVariable<float> currentFuel = new NetworkVariable<float>(500f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float YakitCek(float miktar)
    {
        if (!IsServer) return 0f;

        float cekilecek = Mathf.Min(miktar, currentFuel.Value);
        currentFuel.Value -= cekilecek;
        return cekilecek;
    }

    public void Interact(NetworkObject user)
    {
        // Sol týk mimarisine geçildiði için E tuþu etkileþimi boþ býrakýldý.
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void BidonuDoldurMiktarliServerRpc(ulong oyuncuId, float miktar)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuId, out NetworkObject oyuncuNetObj))
        {
            if (oyuncuNetObj.TryGetComponent(out PlayerInventory inventory))
            {
                float currentBidon = inventory.bidonMevcutYakit.Value;
                float bosYer = 25f - currentBidon;

                if (bosYer > 0 && currentFuel.Value > 0)
                {
                    float transferMiktari = Mathf.Min(miktar, Mathf.Min(bosYer, currentFuel.Value));
                    currentFuel.Value -= transferMiktari;
                    inventory.bidonMevcutYakit.Value += transferMiktari;
                }
            }
        }
    }

    public List<ActionPrompt> GetPrompts()
    {
        List<ActionPrompt> prompts = new List<ActionPrompt>();

        prompts.Add(new ActionPrompt("Ýstasyon Deposu", $"{Mathf.RoundToInt(currentFuel.Value)}L / {Mathf.RoundToInt(maxFuel)}L"));

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            var inventory = playerObj.GetComponent<PlayerInventory>();

            if (inventory != null)
            {
                int activeIdx = inventory.activeHotbarIndex.Value;
                if (!inventory.slots[activeIdx].IsEmpty && inventory.slots[activeIdx].itemData != null)
                {
                    if (inventory.eldekiObje != null && inventory.eldekiObje.GetComponent<YakitBidonu>() != null)
                    {
                        if (inventory.bidonMevcutYakit.Value >= 25f)
                        {
                            prompts.Add(new ActionPrompt("BÝDON DOLU!", "Bidon kapasitesi tamamen dolu"));
                        }
                        else if (currentFuel.Value <= 0f)
                        {
                            prompts.Add(new ActionPrompt("ÝSTASYON BOÞ!", "Ýstasyonda mazot kalmadý"));
                        }
                        else
                        {
                            // --- SOL TIK REHBERLÝÐÝ ---
                            prompts.Add(new ActionPrompt("Sol Týk (Basýlý Tut)", "Bidonu Doldur"));
                        }
                    }
                }
            }
        }

        return prompts;
    }
}