using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DurabilityManager))]
public class BreakDisableBehavior : NetworkBehaviour
{
    public AudioClip breakdownSound;
    public GameObject smokePrefab;
    public Transform smokeSpawnPoint;

    public NetworkVariable<bool> isBroken = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private DurabilityManager durabilityManager;
    private AttachableEquipment equipment;
    private GameObject spawnedSmoke;

    public override void OnNetworkSpawn()
    {
        durabilityManager = GetComponent<DurabilityManager>();
        equipment = GetComponent<AttachableEquipment>();

        durabilityManager.OnHealthEmpty += HandleBreakdown;
        durabilityManager.OnRepaired += HandleRepair;

        isBroken.OnValueChanged += OnBrokenStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (durabilityManager != null)
        {
            durabilityManager.OnHealthEmpty -= HandleBreakdown;
            durabilityManager.OnRepaired -= HandleRepair;
        }
        isBroken.OnValueChanged -= OnBrokenStateChanged;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (isBroken.Value && equipment != null && equipment.isWorking.Value)
        {
            equipment.isWorking.Value = false;
        }
    }

    private void HandleBreakdown()
    {
        if (IsServer)
        {
            isBroken.Value = true;

            var fuelSystem = GetComponent<TractorFuelSystem>();
            if (fuelSystem != null)
            {
                fuelSystem.isEngineRunning.Value = false;
            }

            if (equipment != null)
            {
                equipment.isWorking.Value = false;
            }
        }
    }

    private void HandleRepair()
    {
        if (IsServer)
        {
            isBroken.Value = false;
        }
    }

    private void OnBrokenStateChanged(bool previous, bool current)
    {
        if (current)
        {
            if (breakdownSound != null)
            {
                AudioSource.PlayClipAtPoint(breakdownSound, transform.position);
            }

            AudioSource[] aktifSesler = GetComponents<AudioSource>();
            foreach (AudioSource ses in aktifSesler)
            {
                if (ses.isPlaying)
                {
                    ses.Stop();
                }
            }

            if (smokePrefab != null && spawnedSmoke == null)
            {
                Transform spawnPoint = smokeSpawnPoint != null ? smokeSpawnPoint : transform;
                spawnedSmoke = Instantiate(smokePrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
            }
        }
        else
        {
            if (spawnedSmoke != null)
            {
                Destroy(spawnedSmoke);
            }
        }
    }
}