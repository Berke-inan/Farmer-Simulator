using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Bed : NetworkBehaviour, IInteractable
{
    // Yatağın dolu olup olmadığını tüm ağda takip eder
    private NetworkVariable<bool> isOccupied = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void Interact(NetworkObject playerObject)
    {
        // 1. Meşguliyet ve Gece kontrolü
        if (isOccupied.Value)
        {
            Debug.Log("Bu yatak şu an dolu!");
            return;
        }

        if (!DayNightCycleManager.Instance.IsNight())
        {
            Debug.Log("Sadece gece uyuyabilirsin.");
            return;
        }

        // 2. Sahip kontrolü ve İşlemler
        if (playerObject.IsOwner)
        {
            // Sunucuda yatağı meşgul olarak işaretle
            SetBedOccupiedRpc(true);

            // Oyuncu üzerindeki uyku kontrolcüsünü çalıştır
            if (playerObject.TryGetComponent<PlayerMovement>(out var sleepController))
            {
                sleepController.StartSleeping(this);
            }

            // Mevcut uyku talebi RPC'si
            ulong clientId = playerObject.OwnerClientId;
            DayNightCycleManager.Instance.SendSleepRequestRpc(clientId);
        }


    }
    public List<ActionPrompt> GetPrompts()
    {
        return new List<ActionPrompt>()
        {
            new ActionPrompt("E", "UYU")
        };
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetBedOccupiedRpc(bool occupied)
    {
        isOccupied.Value = occupied;
    }
}