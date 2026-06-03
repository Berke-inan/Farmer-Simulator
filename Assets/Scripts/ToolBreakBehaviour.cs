using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DurabilityManager))]
public class ToolBreakBehavior : NetworkBehaviour
{
    public AudioClip kirilmaSesi;
    private DurabilityManager durabilityManager;

    public override void OnNetworkSpawn()
    {
        durabilityManager = GetComponent<DurabilityManager>();
        durabilityManager.OnHealthEmpty += HandleDestroy;
    }

    public override void OnNetworkDespawn()
    {
        if (durabilityManager != null) durabilityManager.OnHealthEmpty -= HandleDestroy;
    }

    private void HandleDestroy()
    {
        if (kirilmaSesi != null) AudioSource.PlayClipAtPoint(kirilmaSesi, transform.position);

        if (IsServer)
        {
            // Ýleride envanterden de sildirmek istersen inventory.EldeTuketimYapServerRpc() buraya eklenebilir
            NetworkObject.Despawn(true);
        }
    }
}