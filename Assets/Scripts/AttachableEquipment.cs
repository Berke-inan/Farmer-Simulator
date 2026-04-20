using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class AttachableEquipment : NetworkBehaviour
{
    // YENÝ EKLENEN: Ekipman Tipleri
    public enum EquipmentType { Trailer, Header }

    [Header("Ekipman Kimliði")]
    [Tooltip("Bu ekipman arkaya takýlacak bir Römork mu, öne takýlacak bir Biçer mi?")]
    public EquipmentType type;

    [Header("Baðlantý Ayarlarý")]
    public Transform hitchPoint;

    [Header("Fiziksel Tekerlekler (Görünmez)")]
    public WheelCollider[] wheelColliders;

    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform[] visualWheels;

    private Rigidbody rb;
    private Quaternion[] initialOffsets;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 1. KESÝN ÇÖZÜM: Römorkun fiziksel uyku modunu tamamen kapat!
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
}