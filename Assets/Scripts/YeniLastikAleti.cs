using UnityEngine;
using Unity.Netcode;

public class YeniLastikAleti : MonoBehaviour, IUseableTool
{
    public AudioClip takmaSesi;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        DurabilityManager[] parcalar = hit.collider.transform.root.GetComponentsInChildren<DurabilityManager>();

        foreach (DurabilityManager parca in parcalar)
        {
            ModularPartBehavior lastik = parca.GetComponent<ModularPartBehavior>();

            if (lastik != null && parca.currentHealth.Value <= 0)
            {
                if (takmaSesi != null) AudioSource.PlayClipAtPoint(takmaSesi, hit.point);

                parca.RepairFull();
                inventory.EldeTuketimYapServerRpc();
                break;
            }
        }
    }
}