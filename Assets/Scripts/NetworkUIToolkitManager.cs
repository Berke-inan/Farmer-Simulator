using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements; // UI Toolkit kütüphanesi

[RequireComponent(typeof(UIDocument))]
public class NetworkUIToolkitManager : MonoBehaviour
{
    private UIDocument uiDocument;
    private Button hostButton;
    private Button clientButton;
    private VisualElement root;

    private void Awake()
    {
        // Ekrana eklediğimiz UIDocument bileşenini alıyoruz
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // UI Builder'da verdiğimiz isimlerle (Name) butonları buluyoruz
        hostButton = root.Q<Button>("HostButton");
        clientButton = root.Q<Button>("ClientButton");

        // Butonlara tıklanma (clicked) olaylarını bağlıyoruz
        if (hostButton != null) hostButton.clicked += StartHost;
        if (clientButton != null) clientButton.clicked += StartClient;
    }

    private void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        HideMenu();
    }

    private void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        HideMenu();
    }

    private void HideMenu()
    {
        // Oyuna girince arayüzü tamamen gizliyoruz (CSS'deki display: none gibi)
        root.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        // Obje silinirse veya sahne değişirse hafıza sızıntısı olmaması için eventleri temizliyoruz
        if (hostButton != null) hostButton.clicked -= StartHost;
        if (clientButton != null) clientButton.clicked -= StartClient;
    }
}