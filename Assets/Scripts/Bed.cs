using Unity.Netcode;
using UnityEngine;

public class Bed : NetworkBehaviour, IInteractable
{
    public void Interact(NetworkObject playerObject)
    {
        if (!DayNightCycleManager.Instance.IsNight())
        {
            Debug.Log("Şu an gündüz, uyumak için akşam olmasını beklemelisin.");
            return;
        }

        if (playerObject.IsOwner)
        {
            ulong clientId = playerObject.OwnerClientId;
            // Güncellenen RPC çağrısı
            DayNightCycleManager.Instance.SendSleepRequestRpc(clientId);

            Debug.Log("Yatağa yatıldı. Diğer oyuncuların da uyuması bekleniyor...");
        }
    }
}