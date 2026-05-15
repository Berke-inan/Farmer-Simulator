using UnityEngine;

public class OrakEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // Vurduğumuz obje bir ekin mi?
        if (hit.collider.TryGetComponent(out ModularCrop ekin))
        {
            // Ekin büyümüş mü kontrol et
            if (ekin.IsGrown)
            {
                // Hasat komutunu gönder
                inventory.HasatEtServerRpc(ekin.NetworkObjectId, ekin.transform.position);
            }
            else if (ekin.IsRotted)
            {
                Debug.Log("Bu ekin çürümüş!");
            }
            else
            {
                Debug.Log("Bu ekin henüz büyümedi!");
            }
        }
    }
}