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

            if (inventory.TryGetComponent(out PlayerActionManager actionManager))
            {
                int slotIndex = inventory.activeHotbarIndex.Value;

                // Yeni envanter yapısına göre ID çekme
                int itemID = inventory.slots[slotIndex].itemData.itemID;

                actionManager.TohumEkServerRpc(itemID, hit.point, slotIndex);
            }
        }
    }
}