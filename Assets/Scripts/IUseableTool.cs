using UnityEngine;

public interface IUseableTool
{
    // PlayerInventory yerine InventoryManager kullanıldı
    void EylemYap(RaycastHit hit, InventoryManager envanter);
}