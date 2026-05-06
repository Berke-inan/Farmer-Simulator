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
        anaGovde = GetComponent<AttachableEquipment>();

        if (anaGovde == null)
        {
            anaGovde = GetComponentInParent<AttachableEquipment>();
        }
    }

    private void Update()
    {
        if (anaGovde == null || !anaGovde.isWorking.Value) return;

        foreach (Transform demir in demirPivotlari)
        {
            if (demir != null)
            {
                demir.Rotate(donusEkseni * donusHizi * Time.deltaTime, Space.Self);
            }
        }
    }
}