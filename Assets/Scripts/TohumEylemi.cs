using UnityEngine;

public class TohumEylemi : MonoBehaviour, IUseableTool
{
    [Header("Ekim Ayarları")]
    [Tooltip("Başka bir tohuma veya bitkiye ne kadar yaklaşabilir?")]
    public float minimumEkimMesafesi = 0.8f;

    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            // 1. Zemin çapalanmış mı kontrolü
            if (!manager.IsSoilTilled(hit.point))
            {
                Debug.Log("Burası çapalanmamış, ekim yapılamaz.");
                return;
            }

            // 2. Etrafta başka bir ekin var mı kontrolü
            Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
            foreach (var col in yakindakiler)
            {
                if (col.TryGetComponent(out ModularCrop _))
                {
                    Debug.Log("Buraya ekemezsin, başka bir ekine çok yakın!");
                    return;
                }
            }

            // 3. Her şey uygunsa komutu PlayerActionManager'a devret
            if (inv.TryGetComponent(out PlayerActionManager actionManager) && inv.TryGetComponent(out NetworkedHotbar hotbar))
            {
                int slotIndex = hotbar.ActiveSlotIndex.Value;

                // Envanterden o anki slotta tuttuğumuz tohumun ID'sini alıyoruz
                string itemID = inv.Slots[slotIndex].Item.ItemID;

                actionManager.TohumEkServerRpc(itemID, hit.point, slotIndex);
            }
        }
    }
}