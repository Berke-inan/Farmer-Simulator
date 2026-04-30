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

    private void Update()
    {
        // Sadece tekerlekleri döndürür, KLAVYE DİNLEME İŞİ BURADAN SİLİNDİ!
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            if (wheelColliders[i] != null && visualWheels.Length > i && visualWheels[i] != null)
            {
                wheelColliders[i].GetWorldPose(out Vector3 pos, out Quaternion rot);
                visualWheels[i].position = pos;
                visualWheels[i].rotation = rot * initialOffsets[i];
            }
        }
    }

    // DİKKAT: Bu metodu "public" yaptık ki Traktör dışarıdan gelip bu şalteri indirebilsin
    [Rpc(SendTo.Server, RequireOwnership = false)]
    public void CalismayiDegistirServerRpc()
    {
        isWorking.Value = !isWorking.Value;
        Debug.Log(gameObject.name + " çalışma durumu değişti: " + isWorking.Value);
    }
}