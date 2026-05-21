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

        private VisualElement _currentEnergyCircle;
        private Label _energyTextLabel;
        private Label _energyMaxLabel; // Yeni
        private Label _lblMoney;
        private Label _lblTime;
        private Label _lblDay;

        // Çizim Motoru Verileri
        private float _currentEnergyVal = 100f;
        private float _maxEnergyVal = 100f;
        private float _capacityVal = 100f; // Asla geçilemeyecek üst sınır

        // Fotoğraftaki Renkler
        private Color _colorBackground = new Color(0.1f, 0.15f, 0.1f); // Koyu arka plan halkası
        private Color _colorNormal = new Color(0f, 1f, 0.4f); // Parlak yeşil (Güncel)
        private Color _colorCritical = new Color(0.97f, 0.25f, 0.25f); // Kırmızı (Kritik güncel enerji)
        private Color _colorLostMax = new Color(0.15f, 0.3f, 0.2f); // Kaybedilen maksimum enerji (Koyu yeşil/saydam)
        private Color _colorCurrent; // O anki güncel renk

        private List<VisualElement> _uiSlots = new List<VisualElement>();
        private List<Image> _uiIcons = new List<Image>();
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

            if (_currentEnergyCircle != null)
            {
                _currentEnergyCircle.generateVisualContent += DrawRadialEnergyBar;
            }

            CreateHotbarSlots();
        }

        private void Update()
        {
            if (_boundInventory == null && NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                {
                    BindPlayerSystems(localPlayer.gameObject);
                }
            }

            UpdateDynamicStats();
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

                var icon = new Image();
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
            // SAAT GÜNCELLEMESİ
            if (DayNightCycleManager.Instance != null && _lblTime != null)
            {
                float t = DayNightCycleManager.Instance.currentTime.Value;
                int hours = Mathf.FloorToInt(t);
                int minutes = Mathf.FloorToInt((t - hours) * 60f);
                _lblTime.text = $"{hours:00}:{minutes:00}";
            }

            // ENERJİ GÜNCELLEMESİ
            if (_boundEnergy != null && _currentEnergyCircle != null)
            {
                _capacityVal = _boundEnergy.maksimumKapasite; // Örn: 100
                _maxEnergyVal = _boundEnergy.maxEnerji.Value; // Örn: 75
                _currentEnergyVal = _boundEnergy.guncelEnerji.Value; // Örn: 60

                // Renk Belirleme
                if (_currentEnergyVal <= _boundEnergy.eylemYapmaSiniri * 2f)
                    _colorCurrent = _colorCritical;
                else
                    _colorCurrent = _colorNormal;

                // Metinleri Güncelleme (80 / 100)
                if (_energyTextLabel != null) _energyTextLabel.text = ((int)_currentEnergyVal).ToString();
                if (_energyMaxLabel != null) _energyMaxLabel.text = $"/ {(int)_maxEnergyVal}";

                // Çizimi Yenile
                _currentEnergyCircle.MarkDirtyRepaint();
            }

            // PARA GÜNCELLEMESİ
            if (EconomyManager.Instance != null && _lblMoney != null)
            {
                _lblMoney.text = $"$ {EconomyManager.Instance.currentMoney}";
            }
        }

        // ==========================================
        // GÜNCELLEME: DAHA KALIN HALKALAR İLE ÇİZİM MOTORU
        // ==========================================
        private void DrawRadialEnergyBar(MeshGenerationContext ctx)
        {
            var painter = ctx.painter2D;

            // İSTEĞİN: Halkaları kalınlaştırdım (8f -> 14f)
            float lineWidth = 14f;

            painter.lineWidth = lineWidth;
            painter.lineCap = LineCap.Round;

            float width = _currentEnergyCircle.layout.width;
            float height = _currentEnergyCircle.layout.height;

            if (width == 0 || height == 0) return;

            Vector2 center = new Vector2(width / 2f, height / 2f);
            float radius = (Mathf.Min(width, height) / 2f) - (lineWidth / 2f);

            // Açı Hesaplamaları
            float startAngle = -90f;

            // Oranlar (0.0 - 1.0)
            float currentPct = Mathf.Clamp01(_currentEnergyVal / _capacityVal);
            float maxPct = Mathf.Clamp01(_maxEnergyVal / _capacityVal);

            float currentEndAngle = startAngle + (360f * currentPct);
            float maxEndAngle = startAngle + (360f * maxPct);

            // 1. KATMAN: Koyu Arka Plan Halkası (C# ile çiziliyor)
            painter.strokeColor = _colorBackground;
            painter.BeginPath();
            painter.Arc(center, radius, 0f, 360f, ArcDirection.Clockwise);
            painter.Stroke();

            // 2. KATMAN: Kaybedilen Maksimum Enerji Alanı (Koyu Yeşil)
            if (maxPct > currentPct)
            {
                painter.strokeColor = _colorLostMax;
                painter.BeginPath();
                painter.Arc(center, radius, currentEndAngle, maxEndAngle, ArcDirection.Clockwise);
                painter.Stroke();
            }

            // 3. KATMAN: Güncel Enerji Barı (Parlak Yeşil veya Kırmızı)
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
    }
}