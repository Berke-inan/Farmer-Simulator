using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class MarketUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    private VisualElement root;

    // UI Toolkit içindeki butonlar
    private Button closeButton;
    private Button buyButton;
    private Button sellButton;

    private LaptopInteractable currentLaptop;
    private List<int> cart = new List<int>(); // Sepetteki itemID'ler

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Oyun başında UI'ı gizle
        root.style.display = DisplayStyle.None;

        // UI Builder'da (UXML) verdiğin isimlerle butonları yakala
        closeButton = root.Q<Button>("CloseButton");
        buyButton = root.Q<Button>("BuyButton");
        sellButton = root.Q<Button>("SellButton");

        // Tıklama olaylarını bağla
        if (closeButton != null) closeButton.clicked += OnCloseClicked;
        if (buyButton != null) buyButton.clicked += OnBuyClicked;
        if (sellButton != null) sellButton.clicked += OnSellClicked;
    }

    public void OpenUI(LaptopInteractable laptop)
    {
        currentLaptop = laptop;
        cart.Clear(); // Ekran her açıldığında sepeti sıfırla
        root.style.display = DisplayStyle.Flex;
    }

    public void CloseUI()
    {
        root.style.display = DisplayStyle.None;
    }

    private void OnCloseClicked()
    {
        if (currentLaptop != null)
        {
            currentLaptop.ExitLaptop(); // Kamerayı oyuncuya geri gönderir
        }
    }

    // UI'daki ürün listesinden sepete eklemek için bu metodu kullanabilirsin
    public void AddToCart(int itemID)
    {
        cart.Add(itemID);
        Debug.Log($"Sepete eklendi: {itemID}. Toplam ürün: {cart.Count}");
    }

    private void OnBuyClicked()
    {
        if (cart.Count > 0)
        {
            // Sepeti Diziye (Array) çevir ve DeliveryManager'a gönder
            DeliveryManager.Instance.PurchaseCartRpc(cart.ToArray(), NetworkManager.Singleton.LocalClientId);
            cart.Clear();
            OnCloseClicked(); // Satın alma başarılıysa laptoptan çık
        }
        else
        {
            Debug.Log("Sepet boş, önce ürün ekleyin!");
        }
    }

    private void OnSellClicked()
    {
        // Teslimat alanındaki tüm satılabilir eşyaları sat
        DeliveryManager.Instance.SellItemsInZoneRpc();
        OnCloseClicked(); // Satış yapıldıktan sonra laptoptan çık
    }
}