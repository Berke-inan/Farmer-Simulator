using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class TractorFuelSystem : NetworkBehaviour
{
    [Header("Yakýt Ayarlarý")]
    public float maxFuel = 100f;
    public float maxRangeKm = 100f;
    public float idleConsumptionPerSecond = 0.05f; // Rölanti tüketimi
    public float maxSpeedConsumptionPerSecond = 0.3f; // Tam gaz giderken tüketim (YENÝ)

    [Header("Canlý Veriler")]
    public NetworkVariable<float> currentFuel = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // YENÝ: Motorun çalýþýp çalýþmadýðýný aðdaki herkesle senkronize et
    public NetworkVariable<bool> isEngineRunning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Rigidbody rb;
    private TractorController tractorController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        tractorController = GetComponent<TractorController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) currentFuel.Value = maxFuel;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        // Motor Çalýþýyorsa ve Yakýt Varsa Tüketim Yap
        if (isEngineRunning.Value && currentFuel.Value > 0)
        {
            float speed = rb.linearVelocity.magnitude;
            float currentConsumption = idleConsumptionPerSecond; // Baþlangýçta rölanti kadar harcar

            // Gaza basýlýyorsa ve hareket varsa tüketimi hýza göre artýr
            if (Mathf.Abs(tractorController.CurrentGasInput) > 0.05f || speed > 0.5f)
            {
                // Hýzý traktörün tahmini max hýzýyla (örn 15) orantýla
                float speedPercent = Mathf.Clamp01(speed / 15f);
                currentConsumption = Mathf.Lerp(idleConsumptionPerSecond, maxSpeedConsumptionPerSecond, speedPercent);
            }

            // Yakýtý Düþür
            currentFuel.Value -= currentConsumption * Time.fixedDeltaTime;

            if (currentFuel.Value <= 0)
            {
                currentFuel.Value = 0f;
                isEngineRunning.Value = false; // Yakýt bittiyse motoru zorla kapat
                Debug.Log("Traktörün yakýtý bitti, motor durdu!");
            }
        }
    }

    public bool HasFuel => currentFuel.Value > 0f;

    // YENÝ: T Tuþuna basýldýðýnda motoru açýp kapatacak fonksiyon
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ToggleEngineServerRpc()
    {
        if (currentFuel.Value > 0)
        {
            isEngineRunning.Value = !isEngineRunning.Value;
            Debug.Log(isEngineRunning.Value ? "Traktör Motoru ÇALIÞTIRILDI." : "Traktör Motoru DURDURULDU.");
        }
        else
        {
            isEngineRunning.Value = false;
            Debug.Log("Yakýt yok, marþ basmýyor!");
        }
    }

[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddFuelServerRpc(float amount)
    {
        currentFuel.Value = Mathf.Min(currentFuel.Value + amount, maxFuel);
        Debug.Log($"Traktöre {amount} litre yakýt eklendi! Mevcut Yakýt: {currentFuel.Value}");
    }
}