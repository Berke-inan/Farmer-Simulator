using UnityEngine;
using Unity.Netcode;

public class PullukDemirAnimasyonu : NetworkBehaviour
{
    private AttachableEquipment anaGovde;

    [Header("Dönecek Demirler")]
    [Tooltip("5 adet demirin tepe noktalarýný (Pivotlarýný) buraya sürükleyin")]
    public Transform[] demirPivotlari;

    [Header("Dönüþ Ayarlarý")]
    [Tooltip("Dönüþ hýzý. Yavaþ dönmesi için düþük tutuldu.")]
    public float donusHizi = 50f;

    [Tooltip("Hangi eksende dönecek? (Genelde Y ekseni kendi etrafýnda fýrýl fýrýl dönmesidir)")]
    public Vector3 donusEkseni = new Vector3(0, 1, 0);

    private void Awake()
    {
        // Traktörden gelen 'V' tuþu sinyalini (isWorking) okumak için ana gövdeyi buluyoruz
        anaGovde = GetComponent<AttachableEquipment>();

        // Eðer bu kodu yanlýþlýkla alt bir objeye atarsan diye güvenlik önlemi
        if (anaGovde == null)
        {
            anaGovde = GetComponentInParent<AttachableEquipment>();
        }
    }

    private void Update()
    {
        // 1. GÜVENLÝK: Ana gövde bulunamadýysa veya 'V' tuþuna basýlýp makine ÇALIÞTIRILMADIYSA dur!
        if (anaGovde == null || !anaGovde.isWorking.Value) return;

        // 2. DÖNÜÞ: Makine çalýþýyorsa, listedeki tüm demirleri yavaþça döndür
        foreach (Transform demir in demirPivotlari)
        {
            if (demir != null)
            {
                // Space.Self sayesinde demirler traktörün yönüne göre deðil, kendi merkezlerinde döner
                demir.Rotate(donusEkseni * donusHizi * Time.deltaTime, Space.Self);
            }
        }
    }
}