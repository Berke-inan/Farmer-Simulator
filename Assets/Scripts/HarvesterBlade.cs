using UnityEngine;
using Unity.Netcode;

public class HarvesterBlade : NetworkBehaviour
{
    // Býçaðýn üstündeki ana makineyi (Biçerdöver gövdesini) dinleyeceðiz
    private AttachableEquipment anaGovde;

    [Header("Dönüþ Ayarlarý")]
    [Tooltip("Býçaðýn hangi eksende döneceðini belirler. Genelde X (1,0,0) veya Z (0,0,1) olur.")]
    public Vector3 rotationAxis = new Vector3(1, 0, 0);

    [Tooltip("Dönüþ hýzý. Gözüne nasýl güzel geliyorsa ayarlayabilirsin.")]
    public float rotationSpeed = 300f;

    private void Awake()
    {
        // Ana biçerdöver gövdesindeki AttachableEquipment kodunu bul
        anaGovde = GetComponentInParent<AttachableEquipment>();

        if (anaGovde == null)
        {
            Debug.LogError(gameObject.name + " üzerinde AttachableEquipment bulunamadý! Býçak ana gövdenin alt objesi olmalý.");
        }
    }

    private void Update()
    {
        // Ana gövde yoksa dur
        if (anaGovde == null) return;

        // Traktörden V tuþuna basýlýp "Çalýþ" (isWorking = true) emri verildiyse dön!
        if (anaGovde.isWorking.Value)
        {
            // Space.Self sayesinde býçak kendi yerel ekseninde döner
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}