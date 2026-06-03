using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DurabilityManager))]
public class DistanceDamageDealer : NetworkBehaviour
{
    public float distanceThreshold = 50f;
    public float damagePerThreshold = 1f;

    private DurabilityManager durabilityManager;
    private AttachableEquipment equipment;
    private Vector3 lastPosition;
    private float accumulatedDistance = 0f;

    public override void OnNetworkSpawn()
    {
        durabilityManager = GetComponent<DurabilityManager>();
        equipment = GetComponent<AttachableEquipment>();

        if (equipment == null)
        {
            equipment = GetComponentInParent<AttachableEquipment>();
        }

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (equipment != null && !equipment.isWorking.Value)
        {
            lastPosition = transform.position;
            return;
        }

        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        accumulatedDistance += movedDistance;
        lastPosition = transform.position;

        if (accumulatedDistance >= distanceThreshold)
        {
            float damageMultiplier = Mathf.Floor(accumulatedDistance / distanceThreshold);
            durabilityManager.TakeDamage(damagePerThreshold * damageMultiplier);
            accumulatedDistance -= (distanceThreshold * damageMultiplier);
        }
    }
}