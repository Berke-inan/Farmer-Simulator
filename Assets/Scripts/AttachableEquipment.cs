using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem; // Yeni Input Sistemi Kütüphanesi

[RequireComponent(typeof(Rigidbody))]
public class AttachableEquipment : NetworkBehaviour
{
    public enum EquipmentType { Trailer, Header }

    [Header("Ekipman Kimliği")]
    [Tooltip("Bu ekipman arkaya takılacak bir Römork mu, öne takılacak bir Biçer mi?")]
    public EquipmentType type;

    [Header("Bağlantı Ayarları")]
    public Transform hitchPoint;

    [Header("Fiziksel Tekerlekler (Görünmez)")]
    public WheelCollider[] wheelColliders;

    [Header("Görsel Tekerlekler (3D Modeller)")]
    public Transform[] visualWheels;

    // --- ÇALIŞMA DURUMU ---
    [Header("Çalışma Durumu")]
    public NetworkVariable<bool> isWorking = new NetworkVariable<bool>(false);

    private Rigidbody rb;
    private Quaternion[] initialOffsets;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Römorkun fiziksel uyku modunu tamamen kapat!
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

        // --- YENİ INPUT SİSTEMİ İLE V TUŞU KONTROLÜ ---
        // Klavye bağlıysa ve bu karede 'V' tuşuna basıldıysa
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            // Sadece traktöre takılıyken çalışmasını istersen buraya "&& transform.parent != null" şartını ekleyebilirsin.
            CalismayiDegistirServerRpc();
        }
    }

    // RequireOwnership = false sayesinde aleti traktöre takan herkes bu tuşa basabilir
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CalismayiDegistirServerRpc()
    {
        isWorking.Value = !isWorking.Value;
        Debug.Log(gameObject.name + " çalışma durumu değişti: " + isWorking.Value);
    }
}