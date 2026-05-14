using UnityEngine;

public class TohumEylemi : MonoBehaviour, IUseableTool
{
    [Header("Ekim Ayarları")]
    public float minimumEkimMesafesi = 0.8f;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            if (!manager.IsSoilTilled(hit.point))
            {
                Debug.Log("Burası çapalanmamış, ekim yapılamaz.");
                return;
            }

            Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
            foreach (var col in yakindakiler)
            {
                if (col.TryGetComponent(out ModularCrop _))
                {
                    Debug.Log("Buraya ekemezsin, başka bir ekine çok yakın!");
                    return;
                }
            }

            int slotIndex = inventory.activeHotbarIndex.Value;

            // YENİ SİSTEM: Dikme isteğini doğrudan yeni envanter sistemine yolluyoruz
            inventory.DikmeIstegiServerRpc(hit.point, slotIndex);
        }
    }
}