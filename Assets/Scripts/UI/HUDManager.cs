using GLTFast.Schema;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace FarmerSimulator.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _hotbarContainer;

        private VisualElement _currentEnergyCircle;
        private Label _energyTextLabel;
        private Label _energyMaxLabel;
        private Label _lblMoney;
        private Label _lblTime;
        private Label _lblDay;

        private VisualElement _promptContainer;

        private float _currentEnergyVal = 100f;
        private float _maxEnergyVal = 100f;
        private float _capacityVal = 100f;

        private Color _colorBackground = new Color(0.1f, 0.15f, 0.1f);
        private Color _colorNormal = new Color(0f, 1f, 0.4f);
        private Color _colorCritical = new Color(0.97f, 0.25f, 0.25f);
        private Color _colorLostMax = new Color(0.15f, 0.3f, 0.2f);
        private Color _colorCurrent;

        private List<VisualElement> _uiSlots = new List<VisualElement>();
        private List<UnityEngine.UIElements.Image> _uiIcons = new List<UnityEngine.UIElements.Image>();
        private List<Label> _uiAmounts = new List<Label>();

        private PlayerInventory _boundInventory;
        private PlayerEnergy _boundEnergy;

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
            _lblMoney = root.Q<Label>("MoneyText");
            _lblTime = root.Q<Label>("TimeText");
            _lblDay = root.Q<Label>("DayText");

            _currentEnergyCircle = root.Q<VisualElement>("CurrentEnergyCircle");
            _energyTextLabel = root.Q<Label>("EnergyTextLabel");
            _energyMaxLabel = root.Q<Label>("EnergyMaxLabel");

            _promptContainer = root.Q<VisualElement>("ActionPromptContainer");

            if (_currentEnergyCircle != null)
            {
                _currentEnergyCircle.generateVisualContent += DrawRadialEnergyBar;
            }

            CreateHotbarSlots();
        }

        private void Update()
        {
            if (_uiDocument != null && _uiDocument.rootVisualElement != null)
            {
                _uiDocument.rootVisualElement.style.display = MainMenuController.IsMenuOpen ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (MainMenuController.IsMenuOpen) return;

            if (_boundInventory == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                {
                    BindPlayerSystems(localPlayer.gameObject);
                }
            }

            UpdateDynamicStats();

            if (_boundInventory != null && _promptContainer != null && _promptContainer.childCount == 0)
            {
                int activeIdx = _boundInventory.activeHotbarIndex.Value;
                if (!_boundInventory.slots[activeIdx].IsEmpty && _boundInventory.slots[activeIdx].itemData != null)
                {
                    GameObject heldPrefab = _boundInventory.slots[activeIdx].itemData.heldModelPrefab;
                    if (heldPrefab != null && heldPrefab.GetComponent<YakitBidonu>() != null)
                    {
                        UpdateActionPrompts(new List<ActionPrompt>());
                    }
                }
            }
        }

        private void BindPlayerSystems(GameObject player)
        {
            _boundInventory = player.GetComponent<PlayerInventory>();
            _boundEnergy = player.GetComponent<PlayerEnergy>();

            if (_boundInventory != null)
            {
                _boundInventory.OnSlotChanged += UpdateSlotUI;
                _boundInventory.activeHotbarIndex.OnValueChanged += HandleHotbarIndexChanged;

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
                if (i == 3 || i == 6)
                {
                    var divider = new VisualElement();
                    divider.AddToClassList("hotbar-divider");
                    _hotbarContainer.Add(divider);
                }

                var slot = new VisualElement();
                slot.AddToClassList("hotbar-slot");

                var numLabel = new Label();
                numLabel.AddToClassList("slot-number");
                numLabel.text = i < 9 ? (i + 1).ToString() : "0";
                slot.Add(numLabel);

                var icon = new UnityEngine.UIElements.Image();
                icon.AddToClassList("slot-icon");
                slot.Add(icon);

                var amount = new Label();
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
            if (DayNightCycleManager.Instance != null && _lblTime != null)
            {
                float t = DayNightCycleManager.Instance.currentTime.Value;
                int hours = Mathf.FloorToInt(t);
                int minutes = Mathf.FloorToInt((t - hours) * 60f);
                _lblTime.text = $"{hours:00}:{minutes:00}";
            }

            if (_boundEnergy != null && _currentEnergyCircle != null)
            {
                _capacityVal = _boundEnergy.maksimumKapasite;
                _maxEnergyVal = _boundEnergy.maxEnerji.Value;
                _currentEnergyVal = _boundEnergy.guncelEnerji.Value;

                if (_currentEnergyVal <= _boundEnergy.eylemYapmaSiniri * 2f)
                    _colorCurrent = _colorCritical;
                else
                    _colorCurrent = _colorNormal;

                if (_energyTextLabel != null) _energyTextLabel.text = ((int)_currentEnergyVal).ToString();
                if (_energyMaxLabel != null) _energyMaxLabel.text = $"/ {(int)_maxEnergyVal}";

                _currentEnergyCircle.MarkDirtyRepaint();
            }

            if (EconomyManager.Instance != null && _lblMoney != null)
            {
                _lblMoney.text = $"$ {EconomyManager.Instance.currentMoney}";
            }
        }

        public void UpdateActionPrompts(List<ActionPrompt> prompts)
        {
            if (_promptContainer == null) return;

            _promptContainer.Clear();

            if (prompts == null) prompts = new List<ActionPrompt>();

            if (_boundInventory != null)
            {
                int activeIdx = _boundInventory.activeHotbarIndex.Value;
                if (!_boundInventory.slots[activeIdx].IsEmpty && _boundInventory.slots[activeIdx].itemData != null)
                {
                    GameObject heldPrefab = _boundInventory.slots[activeIdx].itemData.heldModelPrefab;
                    if (heldPrefab != null && heldPrefab.GetComponent<YakitBidonu>() != null)
                    {
                        prompts.Insert(0, new ActionPrompt("Bidon Yakıtı", $"{_boundInventory.bidonMevcutYakit.Value:F1}L / 25L"));
                    }
                }
            }

            if (prompts.Count == 0) return;

            foreach (var prompt in prompts)
            {
                var row = new VisualElement();
                row.AddToClassList("prompt-row");

                var actionLabel = new Label(prompt.Action);
                actionLabel.AddToClassList("prompt-action-text");

                var keyBox = new VisualElement();
                keyBox.AddToClassList("prompt-key-box");

                var keyLabel = new Label(prompt.Key);
                keyLabel.AddToClassList("prompt-key-text");

                keyBox.Add(keyLabel);

                row.Add(actionLabel);
                row.Add(keyBox);

                _promptContainer.Add(row);
            }
        }

        private void DrawRadialEnergyBar(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;
            float lineWidth = 14f;

            painter.lineWidth = lineWidth;
            painter.lineCap = LineCap.Round;

            float width = _currentEnergyCircle.layout.width;
            float height = _currentEnergyCircle.layout.height;

            if (width == 0 || height == 0) return;

            Vector2 center = new Vector2(width / 2f, height / 2f);
            float radius = (Mathf.Min(width, height) / 2f) - (lineWidth / 2f);

            float startAngle = -90f;
            float currentPct = Mathf.Clamp01(_currentEnergyVal / _capacityVal);
            float maxPct = Mathf.Clamp01(_maxEnergyVal / _capacityVal);

            float currentEndAngle = startAngle + (360f * currentPct);
            float maxEndAngle = startAngle + (360f * maxPct);

            painter.strokeColor = _colorBackground;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f, ArcDirection.Clockwise);
            painter.Stroke();

            if (maxPct > currentPct)
            {
                painter.strokeColor = _colorLostMax;
                painter.BeginPath();
                painter.Arc(center, radius, currentEndAngle, maxEndAngle, ArcDirection.Clockwise);
                painter.Stroke();
            }

            if (currentPct > 0f)
            {
                painter.strokeColor = _colorCurrent;
                painter.BeginPath();
                painter.Arc(center, radius, startAngle, currentEndAngle, ArcDirection.Clockwise);
                painter.Stroke();
            }
        }

        private void OnDestroy()
        {
            if (_boundInventory != null)
            {
                _boundInventory.OnSlotChanged -= UpdateSlotUI;
                _boundInventory.activeHotbarIndex.OnValueChanged -= HandleHotbarIndexChanged;
            }

            if (_currentEnergyCircle != null)
            {
                _currentEnergyCircle.generateVisualContent -= DrawRadialEnergyBar;
            }
        }

        public void SetPlayerHUDVisible(bool state)
        {
            var displayState = state ? UnityEngine.UIElements.DisplayStyle.Flex : UnityEngine.UIElements.DisplayStyle.None;
            if (_uiDocument == null || _uiDocument.rootVisualElement == null) return;
            var root = _uiDocument.rootVisualElement;

            if (_hotbarContainer != null)
                _hotbarContainer.style.display = displayState;

            var energyWidget = root.Q<UnityEngine.UIElements.VisualElement>("EnergyWidget");
            if (energyWidget != null)
                energyWidget.style.display = displayState;
        }
    }
}