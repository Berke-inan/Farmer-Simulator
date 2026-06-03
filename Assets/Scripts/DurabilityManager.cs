using UnityEngine;
using Unity.Netcode;
using System;

public class DurabilityManager : NetworkBehaviour
{
    [Header("Dayanýklýlýk (Can) Ayarlarý")]
    public float maxHealth = 1000f;

    public NetworkVariable<float> currentHealth = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action OnHealthEmpty;
    public event Action OnRepaired;

    private bool isDead = false;

    public override void OnNetworkSpawn()
    {
        // Sadece obje sýfýrdan doðduysa canýný fulle
        if (IsServer && currentHealth.Value <= 0f)
        {
            currentHealth.Value = maxHealth;
        }

        currentHealth.OnValueChanged += CheckHealthState;
    }

    public void TakeDamage(float amount)
    {
        if (!IsServer || isDead) return;

        currentHealth.Value -= amount;
        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
        }
    }

    private void CheckHealthState(float previousValue, float newValue)
    {
        if (newValue <= 0 && !isDead)
        {
            isDead = true;
            OnHealthEmpty?.Invoke();
        }
    }

    public void RepairFull()
    {
        if (!IsServer) return;

        currentHealth.Value = maxHealth;
        isDead = false;
        RepairClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RepairClientRpc()
    {
        isDead = false;
        OnRepaired?.Invoke();
    }

    // --- YENÝ: EÞYA YERDEN ALINDIÐINDA VEYA ÇANTADAN ÇIKTIÐINDA CANINI GERÝ YÜKLER ---
    public void SetHealth(float health)
    {
        if (!IsServer) return;
        currentHealth.Value = health;
        isDead = (health <= 0);
    }
}