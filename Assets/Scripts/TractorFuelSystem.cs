using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class TractorFuelSystem : NetworkBehaviour
{
    public float maxFuel = 100f;
    public float maxRangeKm = 100f;
    public float idleConsumptionPerSecond = 0.01f;

    public NetworkVariable<float> currentFuel = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Rigidbody rb;
    private TractorController tractorController;
    private float consumptionPerMeter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();
        consumptionPerMeter = maxFuel / (maxRangeKm * 1000f);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentFuel.Value = maxFuel;
    }

    private void FixedUpdate()
    {
        if (!IsServer || currentFuel.Value <= 0) return;

        if (tractorController.IsOccupied)
        {
            float speed = rb.linearVelocity.magnitude;
            float distanceTraveledThisFrame = speed * Time.fixedDeltaTime;

            if (Mathf.Abs(tractorController.CurrentGasInput) > 0.05f && speed > 0.1f)
            {
                currentFuel.Value -= distanceTraveledThisFrame * consumptionPerMeter;
            }
            else
            {
                currentFuel.Value -= idleConsumptionPerSecond * Time.fixedDeltaTime;
            }

            if (currentFuel.Value < 0) currentFuel.Value = 0f;
        }
    }

    public bool HasFuel => currentFuel.Value > 0f;

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void AddFuelServerRpc(float amount)
    {
        currentFuel.Value = Mathf.Min(currentFuel.Value + amount, maxFuel);
        Debug.Log($"Traktöre {amount} litre yakýt eklendi! Mevcut Yakýt: {currentFuel.Value}");
    }
}