using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System için gerekli

public class NetworkedHotbar : NetworkBehaviour
{
    // Aða senkronize aktif slot (Herkes elinde ne tuttuðunu görür)
    public NetworkVariable<int> ActiveSlotIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private Transform handAttachmentPoint;

    // PlayerInteractor'ýn (IUseableTool vb.) eriþebilmesi için public property
    public GameObject CurrentEquippedObject { get; private set; }

    private InventoryManager inventoryManager;

    private void Awake()
    {
        inventoryManager = GetComponent<InventoryManager>();
    }

    public override void OnNetworkSpawn()
    {
        ActiveSlotIndex.OnValueChanged += OnHotbarSlotChanged;

        // Sadece sahibi (Owner) envanteri güncellendiðinde elindeki modeli yenilemeli
        if (IsOwner)
        {
            inventoryManager.OnSlotUpdated += HandleInventoryUpdate;
        }

        UpdateHandModel(ActiveSlotIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        ActiveSlotIndex.OnValueChanged -= OnHotbarSlotChanged;
        if (IsOwner)
        {
            inventoryManager.OnSlotUpdated -= HandleInventoryUpdate;
        }
    }

    private void Update()
    {
        // Tuþ atamalarý sadece bu karakteri kontrol eden oyuncuda çalýþýr
        if (!IsOwner) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectHotbarSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectHotbarSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectHotbarSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectHotbarSlot(3);
        else if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectHotbarSlot(4);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectHotbarSlot(5);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectHotbarSlot(6);
        else if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectHotbarSlot(7);
        else if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectHotbarSlot(8);
        else if (Keyboard.current.digit0Key.wasPressedThisFrame) SelectHotbarSlot(9);
    }

    private void SelectHotbarSlot(int index)
    {
        if (ActiveSlotIndex.Value != index)
        {
            ActiveSlotIndex.Value = index;
        }
    }

    private void HandleInventoryUpdate(int updatedSlotIndex, InventorySlot slotData)
    {
        // Eðer güncellenen slot, elimizde tuttuðumuz slotsa görseli güncelle
        if (updatedSlotIndex == ActiveSlotIndex.Value)
        {
            UpdateHandModel(ActiveSlotIndex.Value);
        }
    }

    private void OnHotbarSlotChanged(int previousValue, int newValue)
    {
        UpdateHandModel(newValue);
    }

    private void UpdateHandModel(int slotIndex)
    {
        if (CurrentEquippedObject != null)
        {
            Destroy(CurrentEquippedObject);
        }

        InventorySlot slot = inventoryManager.Slots[slotIndex];

        if (!slot.IsEmpty && slot.Item.EquipPrefab != null)
        {
            // Objeyi oluþtur (Child olarak baðlamýyoruz, serbest doðuyor)
            CurrentEquippedObject = Instantiate(slot.Item.EquipPrefab);

            // Eðer objede senin takip scriptin varsa hedef olarak handAttachmentPoint'i ver
            if (CurrentEquippedObject.TryGetComponent(out LocalToolTracker tracker))
            {
                tracker.Setup(handAttachmentPoint);
            }
            else
            {
                // Eðer script takmayý unutursan diye eski usul düz baðlama (Yedek plan)
                CurrentEquippedObject.transform.SetParent(handAttachmentPoint);
                CurrentEquippedObject.transform.localPosition = Vector3.zero;
                CurrentEquippedObject.transform.localRotation = Quaternion.identity;
            }
        }
    }
}