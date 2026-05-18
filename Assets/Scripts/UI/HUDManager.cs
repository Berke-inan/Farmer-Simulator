using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using System.Collections.Generic;

namespace FarmerSimulator.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _hotbarContainer;

        // Arayüz elemanlarını hızlı güncellemek için önbellek listeleri
        private List<VisualElement> _uiSlots = new List<VisualElement>();
        private List<Image> _uiIcons = new List<Image>();
        private List<Label> _uiAmounts = new List<Label>();

        private PlayerInventory _boundInventory;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _uiDocument = GetComponent<UIDocument>();
        }

        private void Start()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _hotbarContainer = root.Q<VisualElement>("HotbarContainer");

            CreateHotbarSlots();
        }

        private void Update()
        {
            // Sahnede yerel oyuncu doğduğunda envanter sistemini bağla
            if (_boundInventory == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                {
                    BindPlayerSystems(localPlayer.gameObject);
                }
            }

            // Statları güncellemeye çalışan merkezi fonksiyon
            UpdateDynamicStats();
        }

        private void BindPlayerSystems(GameObject player)
        {
            _boundInventory = player.GetComponent<PlayerInventory>();

            // TODO: Oyuncu enerji scripti atıldığında PlayerEnergy bağlantısı buraya eklenecek.

            if (_boundInventory != null)
            {
                _boundInventory.OnSlotChanged += UpdateSlotUI;
                _boundInventory.activeHotbarIndex.OnValueChanged += HandleHotbarIndexChanged;

                // İlk doğuş anında hotbar durumunu çizdir
                for (int i = 0; i < 10; i++)
                {
                    UpdateSlotUI(i, _boundInventory.slots[i]);
                }
                RefreshActiveSlotHighlight(_boundInventory.activeHotbarIndex.Value);
            }
        }

        private void CreateHotbarSlots()
        {
            _hotbarContainer.Clear();
            _uiSlots.Clear();
            _uiIcons.Clear();
            _uiAmounts.Clear();

            for (int i = 0; i < 10; i++)
            {
                VisualElement slot = new VisualElement();
                slot.AddToClassList("hotbar-slot");

                Image icon = new Image();
                icon.AddToClassList("slot-icon");
                slot.Add(icon);

                Label amount = new Label();
                amount.AddToClassList("slot-amount");
                slot.Add(amount);

                _hotbarContainer.Add(slot);

                _uiSlots.Add(slot);
                _uiIcons.Add(icon);
                _uiAmounts.Add(amount);
            }
        }

        private void UpdateSlotUI(int index, InventorySlot slot)
        {
            if (index >= 10 || _uiIcons.Count <= index) return;

            if (slot == null || slot.IsEmpty)
            {
                _uiIcons[index].sprite = null;
                _uiIcons[index].style.display = DisplayStyle.None;
                _uiAmounts[index].text = "";
            }
            else
            {
                // ÇÖZÜM: ItemData içindeki orijinal field adına (icon) sadık kalındı, hata çözüldü!
                _uiIcons[index].sprite = slot.itemData.icon;
                _uiIcons[index].style.display = DisplayStyle.Flex;
                _uiAmounts[index].text = slot.amount > 1 ? slot.amount.ToString() : "";
            }
        }

        private void HandleHotbarIndexChanged(int oldIdx, int newIdx)
        {
            RefreshActiveSlotHighlight(newIdx);
        }

        private void RefreshActiveSlotHighlight(int activeIndex)
        {
            for (int i = 0; i < _uiSlots.Count; i++)
            {
                if (i == activeIndex)
                    _uiSlots[i].AddToClassList("hotbar-slot-active");
                else
                    _uiSlots[i].RemoveFromClassList("hotbar-slot-active");
            }
        }

        private void UpdateDynamicStats()
        {
            // ==========================================
            // TODO LISTESİ - KODLAR GELDİĞİNDE BAĞLANACAK
            // ==========================================

            // 1. TODO: Enerji barı güncellemesi (Daha sonra bir enerji barı istenirse burası aktif edilecek)
            // 2. TODO: Para güncellemesi (Daha sonra bir para HUD'ı istenirse burası aktif edilecek)
            // 3. TODO: Saat güncellemesi (Daha sonra bir saat HUD'ı istenirse burası aktif edilecek)
        }

        private void OnDestroy()
        {
            if (_boundInventory != null)
            {
                _boundInventory.OnSlotChanged -= UpdateSlotUI;
                _boundInventory.activeHotbarIndex.OnValueChanged -= HandleHotbarIndexChanged;
            }
        }
    }
}