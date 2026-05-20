using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace FarmerSimulator.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ChatController : NetworkBehaviour
    {
        public static ChatController Instance { get; private set; }
        public static bool IsChatOpen { get; private set; } = false;

        [Header("Chat Ayarları")]
        [SerializeField] private float messageVisibilityDuration = 5.0f;

        private UIDocument _uiDocument;
        private VisualElement _chatContainer;
        private ScrollView _chatHistory;
        private TextField _chatInput;

        private InputSystem_Actions _inputActions;
        private float _visibilityTimer = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            _inputActions = new InputSystem_Actions();
        }

        private void Start()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _chatContainer = root.Q<VisualElement>("ChatContainer");
            _chatHistory = root.Q<ScrollView>("ChatHistory");
            _chatInput = root.Q<TextField>("ChatInput");

            _chatInput.RegisterCallback<KeyDownEvent>(OnInputKeyDown);

            _inputActions.Player.Chat.started += ctx => TryOpenChatFromKey();
            _inputActions.Player.Pause.started += ctx => HandlePauseOrCancelAction();

            CloseChat();
        }

        private void OnEnable() => _inputActions?.Enable();
        private void OnDisable() => _inputActions?.Disable();

        private void Update()
        {
            // --- İMLEÇ KORUMA KALKANI ---
            // İSTEĞİN: Chat açıkken başka yere tıklansa bile imlecin kilitlenmesini her kare engeller, geri tıklayabilirsin.
            if (IsChatOpen)
            {
                if (UnityEngine.Cursor.lockState != CursorLockMode.None)
                {
                    UnityEngine.Cursor.lockState = CursorLockMode.None;
                    UnityEngine.Cursor.visible = true;
                }
            }

            if (MainMenuController.IsMenuOpen) return;

            if (_visibilityTimer > 0 && !IsChatOpen)
            {
                _visibilityTimer -= Time.deltaTime;
                if (_visibilityTimer <= 0)
                {
                    _chatHistory?.RemoveFromClassList("chat-history-visible");
                }
            }
        }

        private void TryOpenChatFromKey()
        {
            if (MainMenuController.IsMenuOpen || IsChatOpen) return;
            OpenChat();
        }

        private void HandlePauseOrCancelAction()
        {
            if (MainMenuController.IsMenuOpen) return;

            if (IsChatOpen)
            {
                CloseChat();
            }
            else
            {
                Debug.Log("[Pause Sistem] Chat kapalıyken ESC'ye basıldı. Oyun Durdurma Menüsü tetiklenecek.");
            }
        }

        private void OpenChat()
        {
            IsChatOpen = true;

            _chatContainer?.AddToClassList("chat-container-focused");
            _chatInput?.AddToClassList("chat-input-visible");
            _chatHistory?.AddToClassList("chat-history-visible");

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            TogglePlayerComponents(false);

            _chatInput?.schedule.Execute(() =>
            {
                _chatInput.Focus();
                _chatInput.value = "";
            });
        }

        private void CloseChat()
        {
            IsChatOpen = false;

            _chatContainer?.RemoveFromClassList("chat-container-focused");
            _chatInput?.RemoveFromClassList("chat-input-visible");
            _chatInput?.Blur();

            TogglePlayerComponents(true);

            if (_visibilityTimer <= 0)
            {
                _chatHistory?.RemoveFromClassList("chat-history-visible");
            }

            if (!MainMenuController.IsMenuOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                string message = _chatInput.value?.Trim();
                if (!string.IsNullOrEmpty(message))
                {
                    SendChatMessageServerRpc(message);
                }

                // GÜNCELLEME: PreventDefault yerine yeni Unity 6 standardı olan StopPropagation eklendi
                evt.StopPropagation();

                CloseChat();
            }
        }

        public void AddLocalMessage(string text, string styleClass = "")
        {
            if (_chatHistory == null) return;

            // ÇÖZÜM: TextField yerine artık direkt seçilebilir yerel Label kullanıyoruz (Unity 6 standartı)
            Label newLabel = new Label(text);
            newLabel.AddToClassList("chat-text");

            if (!string.IsNullOrEmpty(styleClass))
            {
                newLabel.AddToClassList(styleClass);
            }

            _chatHistory.Add(newLabel);

            _visibilityTimer = messageVisibilityDuration;
            _chatHistory?.AddToClassList("chat-history-visible");

            _chatHistory.RegisterCallback<GeometryChangedEvent>(ScrollToBottom);
        }

        private void TogglePlayerComponents(bool state)
        {
            if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;

            var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (localPlayer != null)
            {
                var movement = localPlayer.GetComponent<PlayerMovement>();
                if (movement != null) movement.enabled = state;

                var cameraCtrl = localPlayer.GetComponent<PlayerCameraController>();
                if (cameraCtrl != null) cameraCtrl.enabled = state;

                var interactor = localPlayer.GetComponent<PlayerInteractor>();
                if (interactor != null) interactor.enabled = state;

                var playerInput = localPlayer.GetComponent<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null) playerInput.enabled = state;
            }
        }

        private void ScrollToBottom(GeometryChangedEvent evt)
        {
            // Hatalı olan Object atama satırı tamamen silindi!
            _chatHistory.scrollOffset = new Vector2(0, _chatHistory.layout.height);
            _chatHistory.UnregisterCallback<GeometryChangedEvent>(ScrollToBottom);
        }

        [Rpc(SendTo.Server)]
        private void SendChatMessageServerRpc(string message, RpcParams rpcParams = default)
        {
            ulong senderId = rpcParams.Receive.SenderClientId;
            string finalMessage = $"[Oyuncu {senderId}]: {message}";
            ReceiveChatMessageClientRpc(finalMessage);
        }

        [Rpc(SendTo.Everyone)]
        private void ReceiveChatMessageClientRpc(string formattedMessage)
        {
            AddLocalMessage(formattedMessage);
        }
    }
}