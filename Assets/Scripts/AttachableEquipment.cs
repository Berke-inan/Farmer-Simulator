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

    // ==========================================
    // YENİ: OTOMATİK DÜZELTME (KURTARMA) AYARLARI
    // ==========================================
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
        ParkFreniniCek(true);
    }

    private void Update()
    {
        // Sadece tekerlekleri döndürür
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] != null && visualWheels.Length > i && visualWheels[i] != null)
            {
                wheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
                visualWheels[i].position = pos;
                visualWheels[i].rotation = rot * initialOffsets[i];
            }
        }

        // YENİ: Sadece sunucu ters dönme kontrolü yapsın (Ağda senkronizasyon bozulmasın diye)
        if (IsServer)
        {
            TersDonmeKontrolu();
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void CalismayiDegistirServerRpc()
    {
        isWorking.Value = !isWorking.Value;
        Debug.Log(gameObject.name + " çalışma durumu değişti: " + isWorking.Value);
    }

    public void ParkFreniniCek(bool frenCekili)
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
    }

    // ==========================================
    // YENİ EKLENEN: TERS DÖNME ALGISI VE DÜZELTME MANTIĞI
    // ==========================================
    private void TersDonmeKontrolu()
    {
        // Vector3.Dot: Objenin üst yönü (transform.up) ile dünyanın üst yönü (Vector3.up) arasındaki açıyı ölçer.
        // 1 = Tam dik, 0 = Tam yan yatmış, -1 = Tam tepe taklak (ters) dönmüş demektir.
        // Eğer 0.2'den küçükse (yani alet çok fena yan yatmış veya ters dönmüşse) sayacı başlat.
        if (Vector3.Dot(transform.up, Vector3.up) < 0.2f)
        {
            tersDurmaSayaci += Time.deltaTime;

            if (tersDurmaSayaci >= duzelmeSuresi)
            {
                OtomatikDuzelt();
                tersDurmaSayaci = 0f; // Düzelttiğimiz için sayacı sıfırla
            }
        }
        else
        {
            // Eğer alet 3 saniye dolmadan kendi kendine düzelirse, sayacı sıfırla ki haksız yere fırlatmasın.
            tersDurmaSayaci = 0f;
        }
    }

    private void OtomatikDuzelt()
    {
        // 1. Z (Devrilme) ve X (Öne Yatma) açılarını sıfırla, sadece sağa/sola bakış açısını (Y) koru.
        Vector3 mevcutAci = transform.eulerAngles;
        transform.rotation = Quaternion.Euler(0, mevcutAci.y, 0);

        // 2. Yerin içine sıkışmaması için aleti havaya kaldır.
        transform.position += Vector3.up * 1.5f;

        // 3. O anki fırlama ve savrulma momentumlarını sıfırla ki havada uçup gitmesin.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log(gameObject.name + " devrildiği için otomatik olarak ayağa kaldırıldı.");
    }
}