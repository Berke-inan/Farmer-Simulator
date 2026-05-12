using Unity.Netcode;
using UnityEngine;

public class NetworkedHotbar : NetworkBehaviour
{
    private InventoryManager inventoryManager;
    public Transform handAttachmentPoint;

    public NetworkVariable<int> ActiveSlotIndex = new NetworkVariable<int>(0);
    // YENÝ: Elin görsel olarak gizli olup olmadýðýný takip eder
    public NetworkVariable<bool> isHandVisualHidden = new NetworkVariable<bool>(false);

    public GameObject CurrentEquippedObject;

    public override void OnNetworkSpawn()
    {
        inventoryManager = GetComponent<InventoryManager>();

        // Hem slot deðiþtiðinde hem de gizlilik durumu deðiþtiðinde modeli güncelle
        ActiveSlotIndex.OnValueChanged += (oldVal, newVal) => UpdateHandModel();
        isHandVisualHidden.OnValueChanged += (oldVal, newVal) => UpdateHandModel();

        UpdateHandModel();
    }

    // Elin gizlilik durumunu sunucuda deðiþtirir
    [ServerRpc]
    public void SetHandVisualVisibilityServerRpc(bool isHidden)
    {
        isHandVisualHidden.Value = isHidden;
    }

    public void UpdateHandModel()
    {
        // Önce eldekini her zaman temizle
        if (CurrentEquippedObject != null) Destroy(CurrentEquippedObject);

        // ÞART: Eðer el gizlenmiþse veya envanter yüklenmemiþse model oluþturma
        if (isHandVisualHidden.Value || inventoryManager == null) return;

        InventorySlot slot = inventoryManager.Slots[ActiveSlotIndex.Value];

        if (!slot.IsEmpty && slot.Item != null && slot.Item.EquipPrefab != null)
        {
            CurrentEquippedObject = Instantiate(slot.Item.EquipPrefab, handAttachmentPoint);
            // LocalToolTracker veya diðer eklentilerin varsa burada Setup edebilirsin
        }
    }
}