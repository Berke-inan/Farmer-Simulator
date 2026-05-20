using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using FarmerSimulator.Network;
using Unity.Netcode;

namespace FarmerSimulator.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuController : MonoBehaviour
    {
        [Header("Geliştirici Ayarları")]
        [Tooltip("Eğer işaretliyse, editör içinde oyun başladığında menüyü tamamen atlayarak otomatik olarak Host başlatır.")]
        [SerializeField] private bool autoHostInEditor = false;

        // Diğer scriptlerin okuduğu evrensel durum bayrağı
        public static bool IsMenuOpen { get; private set; } = true;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _menuContainer;

        // UI Panelleri
        private VisualElement _panelMainMenu;
        private VisualElement _panelMultiplayer;
        private VisualElement _panelSettings;
        private VisualElement _panelCredits;
        private VisualElement _containerJoin;

        // Sayfa Geçiş Geçmişi
        private readonly Stack<VisualElement> _panelHistory = new();

        // UI Elementleri
        private Button _btnPlay;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnQuit;
        private Button _btnHost;
        private Button _btnJoinMenu;
        private Button _btnConnect;
        private TextField _txtRelayCode;
        private Slider _sliderAudio;

        private const string HideClass = "panel-hidden";

        private void Start()
        {
            _uiDocument = GetComponent<UIDocument>();
            _root = _uiDocument.rootVisualElement;

            if (_root == null) return;

            CacheVisualElements();
            RegisterCallbacks();

            // --- EDİTÖR İÇİN OTOMATİK BAŞLATMA KONTROLÜ ---
#if UNITY_EDITOR
            if (autoHostInEditor)
            {
                BypassUIAndAutoHost();
                return;
            }
#endif

            ResetAllPanels();
        }

        private void Update()
        {
            // Menü açık olduğu sürece imleci koru
            if (IsMenuOpen)
            {
                if (UnityEngine.Cursor.lockState != CursorLockMode.None)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;
                }
            }
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        private void CacheVisualElements()
        {
            _menuContainer = _root.Q<VisualElement>("MenuContainer");
            _panelMainMenu = _root.Q<VisualElement>("MainMenuPanel");
            _panelMultiplayer = _root.Q<VisualElement>("MultiplayerPanel");
            _panelSettings = _root.Q<VisualElement>("SettingsPanel");
            _panelCredits = _root.Q<VisualElement>("CreditsPanel");
            _containerJoin = _root.Q<VisualElement>("JoinContainer");

            _btnPlay = _root.Q<Button>("PlayButton");
            _btnSettings = _root.Q<Button>("SettingsButton");
            _btnCredits = _root.Q<Button>("CreditsButton");
            _btnQuit = _root.Q<Button>("QuitButton");
            _btnHost = _root.Q<Button>("HostButton");
            _btnJoinMenu = _root.Q<Button>("JoinMenuButton");
            _btnConnect = _root.Q<Button>("ConnectButton");
            _txtRelayCode = _root.Q<TextField>("RelayCodeField");
            _sliderAudio = _root.Q<Slider>("AudioSlider");
        }

        private void RegisterCallbacks()
        {
            if (_btnPlay != null) _btnPlay.clicked += () => NavigateToPanel(_panelMultiplayer);
            if (_btnSettings != null) _btnSettings.clicked += () => NavigateToPanel(_panelSettings);
            if (_btnCredits != null) _btnCredits.clicked += () => NavigateToPanel(_panelCredits);
            if (_btnQuit != null) _btnQuit.clicked += HandleQuitGame;

            _root.Q<Button>("MultiplayerBackButton").clicked += NavigateBack;
            _root.Q<Button>("SettingsBackButton").clicked += NavigateBack;
            _root.Q<Button>("CreditsBackButton").clicked += NavigateBack;

            if (_btnHost != null) _btnHost.clicked += HandleHostGame;
            if (_btnJoinMenu != null) _btnJoinMenu.clicked += ToggleJoinInputArea;
            if (_btnConnect != null) _btnConnect.clicked += HandleJoinGame;

            if (_sliderAudio != null) _sliderAudio.RegisterValueChangedCallback(HandleAudioValueChange);
        }

        private void UnregisterCallbacks()
        {
            if (_btnPlay != null) _btnPlay.clicked -= () => NavigateToPanel(_panelMultiplayer);
            if (_btnSettings != null) _btnSettings.clicked -= () => NavigateToPanel(_panelSettings);
            if (_btnCredits != null) _btnCredits.clicked -= () => NavigateToPanel(_panelCredits);
            if (_btnQuit != null) _btnQuit.clicked -= HandleQuitGame;

            _root.Q<Button>("MultiplayerBackButton").clicked -= NavigateBack;
            _root.Q<Button>("SettingsBackButton").clicked -= NavigateBack;
            _root.Q<Button>("CreditsBackButton").clicked -= NavigateBack;

            if (_btnHost != null) _btnHost.clicked -= HandleHostGame;
            if (_btnJoinMenu != null) _btnJoinMenu.clicked -= ToggleJoinInputArea;
            if (_btnConnect != null) _btnConnect.clicked -= HandleJoinGame;

            if (_sliderAudio != null) _sliderAudio.UnregisterValueChangedCallback(HandleAudioValueChange);
        }

        private void ResetAllPanels()
        {
            if (_panelMainMenu == null) return;

            IsMenuOpen = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.Flex;
            _menuContainer?.RemoveFromClassList(HideClass);

            _panelMainMenu.RemoveFromClassList(HideClass);
            _panelMultiplayer?.AddToClassList(HideClass);
            _panelSettings?.AddToClassList(HideClass);
            _panelCredits?.AddToClassList(HideClass);
            _containerJoin?.AddToClassList(HideClass);

            _panelHistory.Clear();
            _panelHistory.Push(_panelMainMenu);
        }

        /// <summary>
        /// Manuel geçişlerde butona basıldığı an direkt oyunu başlatan fonksiyon.
        /// </summary>
        private async void HandleHostGame()
        {
            if (RelayManager.Instance == null || _btnHost == null) return;

            _btnHost.SetEnabled(false);

            // Relay ağ kurulumunu başlatır
            string code = await RelayManager.Instance.SetupAndStartRelay(3);

            if (!string.IsNullOrEmpty(code))
            {
                CloseMenuAndStartPlaying();

                // Üretilen kodu oyun içi sistem chatinize anında basar
                if (ChatController.Instance != null)
                {
                    ChatController.Instance.AddLocalMessage($"[SİSTEM] Oda Kuruldu! Arkadaşınız için Relay Kodu: {code}", "chat-style-system");
                }
            }
            else
            {
                _btnHost.SetEnabled(true);
            }
        }

        /// <summary>
        /// Editör modunda menüyü tamamen bypass ederek direkt oyunu kuran arka plan fonksiyonu.
        /// </summary>
        private async void BypassUIAndAutoHost()
        {
            // Menüyü anında görünmez yap ve kilitleri kaldır
            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.None;

            IsMenuOpen = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            if (RelayManager.Instance != null)
            {
                string code = await RelayManager.Instance.SetupAndStartRelay(3);
                Debug.Log($"[AutoHost] Menü bypass edildi, oyun arkada otomatik kuruldu. Kod: {code}");
            }
        }

        private void CloseMenuAndStartPlaying()
        {
            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.None;

            IsMenuOpen = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void ToggleJoinInputArea()
        {
            if (_containerJoin == null) return;

            if (_containerJoin.ClassListContains(HideClass))
                _containerJoin.RemoveFromClassList(HideClass);
            else
                _containerJoin.AddToClassList(HideClass);
        }

        private async void HandleJoinGame()
        {
            if (_txtRelayCode == null || _btnConnect == null) return;

            string insertedCode = _txtRelayCode.value?.Trim();
            if (string.IsNullOrEmpty(insertedCode) || insertedCode.Length < 6) return;

            _btnConnect.SetEnabled(false);
            bool isSuccess = await RelayManager.Instance.JoinRelay(insertedCode);

            if (isSuccess)
            {
                CloseMenuAndStartPlaying();
            }
            else
            {
                _btnConnect.SetEnabled(true);
            }
        }

        private void NavigateToPanel(VisualElement targetPanel)
        {
            if (targetPanel == null) return;
            if (_panelHistory.Count > 0) _panelHistory.Peek()?.AddToClassList(HideClass);

            targetPanel.RemoveFromClassList(HideClass);
            _panelHistory.Push(targetPanel);
        }

        private void NavigateBack()
        {
            if (_panelHistory.Count <= 1) return;
            _panelHistory.Pop()?.AddToClassList(HideClass);
            _panelHistory.Peek()?.RemoveFromClassList(HideClass);
        }

        private void HandleQuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void HandleAudioValueChange(ChangeEvent<float> evt)
        {
            Debug.Log($"[Ayarlar] Ana Ses: %{evt.newValue:F0}");
        }
    }
}