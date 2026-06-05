using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using FarmerSimulator.Network;
using Unity.Netcode;
using UnityEngine.InputSystem; // YENİ INPUT SİSTEMİ EKLENDİ

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

        private InputSystem_Actions _inputActions;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _menuContainer;

        // UI Panelleri
        private VisualElement _panelMainMenu;
        private VisualElement _panelMultiplayer;
        private VisualElement _panelSettings;
        private VisualElement _panelCredits;
        private VisualElement _containerJoin;
        private VisualElement _panelPause;             // YENİ PAUSE PANELİ
        private VisualElement _panelConfirmDisconnect; // YENİ ÇIKIŞ ONAY PANELİ

        // Sayfa Geçiş Geçmişi
        private readonly Stack<VisualElement> _panelHistory = new();

        // UI Elementleri (Ana Menü)
        private Button _btnPlay;
        private Button _btnSettings;
        private Button _btnCredits;
        private Button _btnQuit;
        private Button _btnHost;
        private Button _btnJoinMenu;
        private Button _btnConnect;
        private TextField _txtRelayCode;
        private Slider _sliderAudio;

        // UI Elementleri (Pause Menü)
        private Button _btnResume;
        private Button _btnPauseSettings;
        private Button _btnDisconnectRequest;
        private Button _btnConfirmDisconnect;
        private Button _btnCancelDisconnect;

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

        private void Awake()
        {
            _inputActions = new InputSystem_Actions();

            // "Pause" action'ı tetiklendiğinde OnPausePressed metodunu çalıştır
            _inputActions.Player.Pause.started += OnPausePressed;
        }

        private void OnEnable()
        {
            _inputActions?.Enable();
        }
        private void OnDisable()
        {
            _inputActions?.Disable();
        }

        private void OnPausePressed(InputAction.CallbackContext context)
        {
            // KESİN ÇÖZÜM: Eğer market/laptop ekranı şu an açıksa, ESC menüsünü AÇMA!
            if (MarketUIController.IsMarketOpen) return;

            // Sadece ağa bağlıysak (oyun içindeysek) ESC ile menüyü aç/kapat
            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            {
                TogglePauseMenu();
            }
        }

        private void Update()
        {
            // Menü açık olduğu sürece imleci serbest bırak, kapalıysa kilitle
            if (IsMenuOpen)
            {
                if (UnityEngine.Cursor.lockState != CursorLockMode.None)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;
                }
            }
            else
            {
                if (UnityEngine.Cursor.lockState != CursorLockMode.Locked)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                    UnityEngine.Cursor.visible = false;
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

            // Paneller
            _panelMainMenu = _root.Q<VisualElement>("MainMenuPanel");
            _panelMultiplayer = _root.Q<VisualElement>("MultiplayerPanel");
            _panelSettings = _root.Q<VisualElement>("SettingsPanel");
            _panelCredits = _root.Q<VisualElement>("CreditsPanel");
            _containerJoin = _root.Q<VisualElement>("JoinContainer");
            _panelPause = _root.Q<VisualElement>("PausePanel");
            _panelConfirmDisconnect = _root.Q<VisualElement>("ConfirmDisconnectPanel");

            // Ana Menü Butonları
            _btnPlay = _root.Q<Button>("PlayButton");
            _btnSettings = _root.Q<Button>("SettingsButton");
            _btnCredits = _root.Q<Button>("CreditsButton");
            _btnQuit = _root.Q<Button>("QuitButton");
            _btnHost = _root.Q<Button>("HostButton");
            _btnJoinMenu = _root.Q<Button>("JoinMenuButton");
            _btnConnect = _root.Q<Button>("ConnectButton");
            _txtRelayCode = _root.Q<TextField>("RelayCodeField");
            _sliderAudio = _root.Q<Slider>("AudioSlider");

            // Pause Menü Butonları
            _btnResume = _root.Q<Button>("ResumeButton");
            _btnPauseSettings = _root.Q<Button>("PauseSettingsButton");
            _btnDisconnectRequest = _root.Q<Button>("DisconnectRequestButton");
            _btnConfirmDisconnect = _root.Q<Button>("ConfirmDisconnectButton");
            _btnCancelDisconnect = _root.Q<Button>("CancelDisconnectButton");
        }

        private void RegisterCallbacks()
        {
            // Ana Menü Yönlendirmeleri
            if (_btnPlay != null) _btnPlay.clicked += () => NavigateToPanel(_panelMultiplayer);
            if (_btnSettings != null) _btnSettings.clicked += () => NavigateToPanel(_panelSettings);
            if (_btnCredits != null) _btnCredits.clicked += () => NavigateToPanel(_panelCredits);
            if (_btnQuit != null) _btnQuit.clicked += HandleQuitGame;

            // Geri Dön Butonları (Güvenli Kontrol)
            var btnMultiBack = _root.Q<Button>("MultiplayerBackButton");
            if (btnMultiBack != null) btnMultiBack.clicked += NavigateBack;

            var btnSetBack = _root.Q<Button>("SettingsBackButton");
            if (btnSetBack != null) btnSetBack.clicked += NavigateBack;

            var btnCredBack = _root.Q<Button>("CreditsBackButton");
            if (btnCredBack != null) btnCredBack.clicked += NavigateBack;

            if (_btnHost != null) _btnHost.clicked += HandleHostGame;
            if (_btnJoinMenu != null) _btnJoinMenu.clicked += ToggleJoinInputArea;
            if (_btnConnect != null) _btnConnect.clicked += HandleJoinGame;

            if (_sliderAudio != null) _sliderAudio.RegisterValueChangedCallback(HandleAudioValueChange);

            // Pause Menü Yönlendirmeleri
            if (_btnResume != null) _btnResume.clicked += TogglePauseMenu;
            if (_btnPauseSettings != null) _btnPauseSettings.clicked += () => NavigateToPanel(_panelSettings);
            if (_btnDisconnectRequest != null) _btnDisconnectRequest.clicked += () => NavigateToPanel(_panelConfirmDisconnect);
            if (_btnCancelDisconnect != null) _btnCancelDisconnect.clicked += NavigateBack;
            if (_btnConfirmDisconnect != null) _btnConfirmDisconnect.clicked += ExecuteDisconnect;
        }

        private void UnregisterCallbacks()
        {
            if (_btnPlay != null) _btnPlay.clicked -= () => NavigateToPanel(_panelMultiplayer);
            if (_btnSettings != null) _btnSettings.clicked -= () => NavigateToPanel(_panelSettings);
            if (_btnCredits != null) _btnCredits.clicked -= () => NavigateToPanel(_panelCredits);
            if (_btnQuit != null) _btnQuit.clicked -= HandleQuitGame;

            // Geri Dön Butonları (Güvenli Kontrol)
            var btnMultiBack = _root.Q<Button>("MultiplayerBackButton");
            if (btnMultiBack != null) btnMultiBack.clicked -= NavigateBack;

            var btnSetBack = _root.Q<Button>("SettingsBackButton");
            if (btnSetBack != null) btnSetBack.clicked -= NavigateBack;

            var btnCredBack = _root.Q<Button>("CreditsBackButton");
            if (btnCredBack != null) btnCredBack.clicked -= NavigateBack;

            if (_btnHost != null) _btnHost.clicked -= HandleHostGame;
            if (_btnJoinMenu != null) _btnJoinMenu.clicked -= ToggleJoinInputArea;
            if (_btnConnect != null) _btnConnect.clicked -= HandleJoinGame;

            if (_sliderAudio != null) _sliderAudio.UnregisterValueChangedCallback(HandleAudioValueChange);

            if (_btnResume != null) _btnResume.clicked -= TogglePauseMenu;
            if (_btnPauseSettings != null) _btnPauseSettings.clicked -= () => NavigateToPanel(_panelSettings);
            if (_btnDisconnectRequest != null) _btnDisconnectRequest.clicked -= () => NavigateToPanel(_panelConfirmDisconnect);
            if (_btnCancelDisconnect != null) _btnCancelDisconnect.clicked -= NavigateBack;
            if (_btnConfirmDisconnect != null) _btnConfirmDisconnect.clicked -= ExecuteDisconnect;
        }
        private void ResetAllPanels()
        {
            if (_panelMainMenu == null) return;

            IsMenuOpen = true;

            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.Flex;
            _menuContainer?.RemoveFromClassList(HideClass);

            _panelMainMenu.RemoveFromClassList(HideClass);
            _panelMultiplayer?.AddToClassList(HideClass);
            _panelSettings?.AddToClassList(HideClass);
            _panelCredits?.AddToClassList(HideClass);
            _containerJoin?.AddToClassList(HideClass);
            _panelPause?.AddToClassList(HideClass);
            _panelConfirmDisconnect?.AddToClassList(HideClass);

            _panelHistory.Clear();
            _panelHistory.Push(_panelMainMenu);
        }

        // ==========================================
        // YENİ: OYUN İÇİ ESC KONTROLÜ
        // ==========================================
        private void TogglePauseMenu()
        {
            if (!IsMenuOpen)
            {
                // Menüyü Aç (Oyun arkada akmaya devam eder)
                IsMenuOpen = true;
                if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.Flex;
                _menuContainer?.RemoveFromClassList(HideClass);

                // Diğer her şeyi gizle, sadece Pause panelini göster
                _panelMainMenu?.AddToClassList(HideClass);
                _panelMultiplayer?.AddToClassList(HideClass);
                _panelSettings?.AddToClassList(HideClass);
                _panelCredits?.AddToClassList(HideClass);
                _containerJoin?.AddToClassList(HideClass);
                _panelConfirmDisconnect?.AddToClassList(HideClass);

                _panelPause.RemoveFromClassList(HideClass);

                _panelHistory.Clear();
                _panelHistory.Push(_panelPause);
            }
            else
            {
                // Oyuna Dön
                CloseMenuAndStartPlaying();
            }
        }

        // ==========================================
        // YENİ: BAĞLANTIYI KESME VE SIFIRLAMA
        // ==========================================
        private void ExecuteDisconnect()
        {
            // Sunucu/İstemci bağlantısını kopar
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // Geliştirici Notu: Eğer oyunu sıfırlarken eski veriler kalsın istemiyorsan, 
            // buraya sahneyi yeniden yükleme kodu ekleyebilirsin:
            // UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

            // Host/Connect butonlarının kilitlerini aç
            _btnHost?.SetEnabled(true);
            _btnConnect?.SetEnabled(true);

            // Ana menüye sıfırla
            ResetAllPanels();
        }

        private async void HandleHostGame()
        {
            if (RelayManager.Instance == null || _btnHost == null) return;

            _btnHost.SetEnabled(false);

            string code = await RelayManager.Instance.SetupAndStartRelay(3);

            if (!string.IsNullOrEmpty(code))
            {
                CloseMenuAndStartPlaying();

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

        private async void BypassUIAndAutoHost()
        {
            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.None;

            IsMenuOpen = false;

            if (RelayManager.Instance != null)
            {
                string code = await RelayManager.Instance.SetupAndStartRelay(3);
                Debug.Log($"[AutoHost] Menü bypass edildi, oyun arkada otomatik kuruldu. Kod: {code}");
            }
        }

        private void CloseMenuAndStartPlaying()
        {
            if (_menuContainer != null) _menuContainer.style.display = DisplayStyle.None;
            _menuContainer?.AddToClassList(HideClass);

            IsMenuOpen = false;
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