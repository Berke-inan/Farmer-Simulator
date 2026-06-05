using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class AttachableEquipment : NetworkBehaviour
{
    public enum EquipmentType { Trailer, Header }

    [Header("Ekipman Kimliği")]
    public EquipmentType type;

    [Header("Bağlantı Ayarları")]
    public Transform hitchPoint;

    [Header("Fiziksel Tekerlekler")]
    public WheelCollider[] wheelColliders;

    [Header("Görsel Tekerlekler")]
    public Transform[] visualWheels;

    [Header("Çalışma Durumu")]
    public NetworkVariable<bool> isWorking = new NetworkVariable<bool>(false);

    [Header("Devrilme Kurtarma")]
    [Tooltip("Alet kaç saniye ters kalırsa otomatik düzeltilsin?")]
    public float duzelmeSuresi = 3f;
    private float tersDurmaSayaci = 0f;

    private Rigidbody rb;
    private Quaternion[] initialOffsets;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.sleepThreshold = 0f;

        initialOffsets = new Quaternion[wheelColliders.Length];

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] != null && visualWheels.Length > i && visualWheels[i] != null)
            {
                wheelColliders[i].GetWorldPose(out Vector3 startPos, out Quaternion startRot);
                Quaternion visualStartRot = visualWheels[i].rotation;
                initialOffsets[i] = Quaternion.Inverse(startRot) * visualStartRot;
            }
        }
    }

    private void Start()
    {
        // İlk doğduğunda el frenini güvenli modda çek
        ParkFreniniCek(true);
    }

    private void Update()
    {
        // Tekerleklerin dönme animasyon senkronizasyonu
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] != null && visualWheels.Length > i && visualWheels[i] != null)
            {
                wheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
                visualWheels[i].position = pos;
                visualWheels[i].rotation = rot * initialOffsets[i];
            }
        }

        // Fiziksel dünya durumlarını sadece Sunucu (Server) denetler
        if (IsServer)
        {
            TersDonmeKontrolu();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CalismayiDegistirServerRpc()
    {
        isWorking.Value = !isWorking.Value;
        Debug.Log(gameObject.name + " çalışma durumu değişti: " + isWorking.Value);
    }

    // --- %100 MULTIPLAYER SAFE FREN MOTORU ---
    // Bu fonksiyon artık projenin neresinden çağrılırsa çağrılsın ağ durumunu otomatik kontrol eder
    public void ParkFreniniCek(bool frenCekili)
    {
        if (IsServer)
        {
            ExecuteParkFreniniCek(frenCekili);
        }
        else
        {
            // Eğer bir Client aleti bırakmaya (Dismount) çalışıyorsa önce Server'a bildirir
            SetParkBrakeServerRpc(frenCekili);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetParkBrakeServerRpc(bool frenCekili)
    {
        ExecuteParkFreniniCek(frenCekili);
    }

    // Fiziksel olarak Rigidbody ve WheelCollider'ları güncelleyen ana gövde
    private void ExecuteParkFreniniCek(bool frenCekili)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearDamping = frenCekili ? 5f : 0f;
            rb.angularDamping = frenCekili ? 5f : 0.05f;
        }

        if (wheelColliders != null && wheelColliders.Length > 0)
        {
            foreach (WheelCollider teker in wheelColliders)
            {
                if (teker != null)
                {
                    teker.brakeTorque = frenCekili ? 10000f : 0f;
                    teker.motorTorque = 0f;
                }
            }
        }

        // Değişikliği tüm istemcilerin (Client) yerel RAM simülasyonuna da anlık bildiriyoruz
        SyncParkBrakeClientRpc(frenCekili);
    }

    [Rpc(SendTo.Everyone)]
    private void SyncParkBrakeClientRpc(bool frenCekili)
    {
        if (IsServer) return; // Sunucu zaten üstteki ana gövdede işledi

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearDamping = frenCekili ? 5f : 0f;
            rb.angularDamping = frenCekili ? 5f : 0.05f;
        }

        if (wheelColliders != null && wheelColliders.Length > 0)
        {
            foreach (WheelCollider teker in wheelColliders)
            {
                if (teker != null) teker.brakeTorque = frenCekili ? 10000f : 0f;
            }
        }
    }

    // --- TERS DÖNME ALGISI VE DÜZELTME MANTIĞI (TAMAMLANDI) ---
    private void TersDonmeKontrolu()
    {
        // Objenin üst yönü ile dünyanın üst yönü arasındaki açıyı ölçer (< 0.2f = yan/ters dönmüş)
        if (Vector3.Dot(transform.up, Vector3.up) < 0.2f)
        {
            tersDurmaSayaci += Time.deltaTime;

            if (tersDurmaSayaci >= duzelmeSuresi)
            {
                OtomatikDuzelt();
                tersDurmaSayaci = 0f;
            }
        }
        else
        {
            tersDurmaSayaci = 0f;
        }
    }

    private void OtomatikDuzelt()
    {
        // Açıları sıfırla, sadece Y (sağa/sola bakış) açısını koru
        Vector3 mevcutAci = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, mevcutAci.y, 0);

        // Yerin içine sıkışmaması için hafifçe yukarı kaldır
        transform.position += Vector3.up * 1.5f;

        // Savrulma momentumlarını sıfırla ki fırlamasın
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log(gameObject.name + " devrildiği için otomatik olarak ayağa kaldırıldı.");
    }
}