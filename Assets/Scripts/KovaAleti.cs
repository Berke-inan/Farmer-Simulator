using UnityEngine;
using Unity.Netcode;

public class KovaAleti : MonoBehaviour, IUseableTool
{
    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // Iþýnýn çarptýðý objede doðrudan Ýnek veya Þiþe kodlarýný arýyoruz (Kafa karýþýklýðýna son!)
        CowInteractable inek = hit.collider.GetComponentInParent<CowInteractable>();
        BottleInteractable sise = hit.collider.GetComponentInParent<BottleInteractable>();

        NetworkObject playerNetObj = inventory.GetComponent<NetworkObject>();

        // Eðer çarptýðýmýz þey Ýnek ise onu sað
        if (inek != null)
        {
            inek.Interact(playerNetObj);
        }
        // Eðer çarptýðýmýz þey Þiþe ise onu doldur
        else if (sise != null)
        {
            sise.Interact(playerNetObj);
        }
        // Ýkisi de deðilse hiçbir þey yapma (Güvenlik)
        else
        {
            Debug.Log("Kova sadece Ýnek ve Þiþe'de çalýþýr. Vurulan: " + hit.collider.gameObject.name);
        }
    }
}