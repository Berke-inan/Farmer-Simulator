using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DurabilityManager), typeof(BreakDisableBehavior))]
public class RepairableTarget : NetworkBehaviour
{
    public int requiredHitsToRepair = 3;
    private int currentHits = 0;
    private DurabilityManager durabilityManager;
    private BreakDisableBehavior disableBehavior;

    public override void OnNetworkSpawn()
    {
        durabilityManager = GetComponent<DurabilityManager>();
        disableBehavior = GetComponent<BreakDisableBehavior>();

        durabilityManager.OnHealthEmpty += ResetHits;
    }

    public override void OnNetworkDespawn()
    {
        if (durabilityManager != null)
        {
            durabilityManager.OnHealthEmpty -= ResetHits;
        }
    }

    private void ResetHits()
    {
        currentHits = 0;
    }

    public void ReceiveRepairHit()
    {
        if (!IsServer || !disableBehavior.isBroken.Value) return;

        currentHits++;
        if (currentHits >= requiredHitsToRepair)
        {
            durabilityManager.RepairFull();
            currentHits = 0;
        }
    }
}