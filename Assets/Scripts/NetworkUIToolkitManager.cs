using UnityEngine;
using UnityEngine.UIElements;

public class NetworkUIManager : MonoBehaviour
{
    private VisualElement root;
    private Button hostBtn;
    private Button continueBtn;
    private TextField joinCodeField;
    private Label displayCodeLabel;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        // UXML'deki isimlerle çekiyoruz
        hostBtn = root.Q<Button>("HostButton");
        continueBtn = root.Q<Button>("ContinueButton");
        joinCodeField = root.Q<TextField>("JoinCodeField");
        displayCodeLabel = root.Q<Label>("DisplayCodeLabel");

        hostBtn.clicked += OnHostClicked;
        continueBtn.clicked += OnContinueClicked;
        root.Q<Button>("ClientButton").clicked += OnClientClicked;
    }

    private async void OnHostClicked()
    {
        // 1. Relay'i kur ve StartHost'u arkada çalıştır
        string code = await RelayManager.Instance.SetupAndStartRelay();

        if (!string.IsNullOrEmpty(code))
        {
            // 2. Kodu göster ve panoya kopyala
            displayCodeLabel.text = "KOD: " + code;
            GUIUtility.systemCopyBuffer = code;

            // 3. UI'ı değiştir: Host butonu gitsin, Devam (Kapat) butonu gelsin
            hostBtn.style.display = DisplayStyle.None;
            continueBtn.style.display = DisplayStyle.Flex;

            Debug.Log("Kodu arkadaşına atabilirsin, oyun hazır!");
        }
    }

    private void OnContinueClicked()
    {
        // Oyun zaten arkada açık, biz sadece menüyü gizliyoruz!
        root.style.display = DisplayStyle.None;
    }

    private void OnClientClicked()
    {
        string inputCode = joinCodeField.value;
        if (!string.IsNullOrEmpty(inputCode))
        {
            RelayManager.Instance.JoinRelay(inputCode);
            root.style.display = DisplayStyle.None; // Katılınca da menüyü kapat
        }
    }
}