using UnityEngine;
using UnityEngine.UIElements;
using Unity.Services.Core; // Unity Services için eklendi
using Unity.Services.Authentication; // Relay kimlik doğrulaması için eklendi

public class NetworkUIManager : MonoBehaviour
{
    [Header("Test Ayarları")]
    [Tooltip("Aktif edildiğinde oyun başlar başlamaz otomatik Host kurar ve UI'ı gizler.")]
    public bool autoStartHostForTesting = false;

    private VisualElement root;
    private Button hostBtn;
    private Button continueBtn;
    private TextField joinCodeField;
    private Label displayCodeLabel;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        hostBtn = root.Q<Button>("HostButton");
        continueBtn = root.Q<Button>("ContinueButton");
        joinCodeField = root.Q<TextField>("JoinCodeField");
        displayCodeLabel = root.Q<Label>("DisplayCodeLabel");

        hostBtn.clicked += OnHostClicked;
        continueBtn.clicked += OnContinueClicked;
        root.Q<Button>("ClientButton").clicked += OnClientClicked;
    }

    private async void Start()
    {
        if (autoStartHostForTesting)
        {
            try
            {
                // 1. Unity Services henüz başlatılmadıysa başlat
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    await UnityServices.InitializeAsync();
                }

                // 2. Relay hizmeti anonim giriş gerektirir, yapılmadıysa giriş yap
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                Debug.Log("Test Modu: Servisler hazır, Host başlatılıyor...");
                OnHostClicked();
            }
            catch (System.Exception e)
            {
                Debug.LogError("Otomatik başlatma sırasında hata: " + e.Message);
            }
        }
    }

    private async void OnHostClicked()
    {
        string code = await RelayManager.Instance.SetupAndStartRelay();

        if (!string.IsNullOrEmpty(code))
        {
            displayCodeLabel.text = "KOD: " + code;
            GUIUtility.systemCopyBuffer = code;

            hostBtn.style.display = DisplayStyle.None;
            continueBtn.style.display = DisplayStyle.Flex;

            Debug.Log("Kodu arkadaşına atabilirsin, oyun hazır!");

            if (autoStartHostForTesting)
            {
                root.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnContinueClicked()
    {
        root.style.display = DisplayStyle.None;
    }

    private void OnClientClicked()
    {
        string inputCode = joinCodeField.value;
        if (!string.IsNullOrEmpty(inputCode))
        {
            RelayManager.Instance.JoinRelay(inputCode);
            root.style.display = DisplayStyle.None;
        }
    }
}