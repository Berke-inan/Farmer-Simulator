using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using System.Collections;
using UnityEngine.InputSystem;

public class MarketUIController : MonoBehaviour
{
    public UIDocument uiDocument;
    private VisualElement root, buyPage, cartPage, sellPage;
    private Button buyTab, cartTab, sellTab;
    private ScrollView marketList, cartList, sellList;
    private Label cartTotal, sellTotal, balanceLabel, notifyLabel;

    // SEPET MANTIĞI: ItemID -> (Veri, Adet)
    private Dictionary<int, (MarketItem data, int qty)> cartData = new Dictionary<int, (MarketItem, int)>();

    // SATIŞ MANTIĞI
    private Dictionary<int, int> sellQuantities = new Dictionary<int, int>();
    private Dictionary<int, List<SellableItem>> itemsInZoneGrouped = new Dictionary<int, List<SellableItem>>();

    private LaptopInteractable currentLaptop;

    private void Awake()
    {
        root = uiDocument.rootVisualElement;

        buyPage = root.Q<VisualElement>("BuyPage");
        cartPage = root.Q<VisualElement>("CartPage");
        sellPage = root.Q<VisualElement>("SellPage");
        buyTab = root.Q<Button>("BuyTabBtn");
        cartTab = root.Q<Button>("CartTabBtn");
        sellTab = root.Q<Button>("SellTabBtn");
        marketList = root.Q<ScrollView>("MarketList");
        cartList = root.Q<ScrollView>("CartList");
        sellList = root.Q<ScrollView>("SellList");
        cartTotal = root.Q<Label>("CartTotalLabel");
        sellTotal = root.Q<Label>("SellTotalLabel");
        balanceLabel = root.Q<Label>("BalanceLabel");
        notifyLabel = root.Q<Label>("NotificationLabel");

        buyTab.clicked += () => SwitchPage(0);
        cartTab.clicked += () => SwitchPage(1);
        sellTab.clicked += () => SwitchPage(2);

        root.Q<Button>("CloseButton").clicked += () => currentLaptop?.ExitLaptop();
        root.Q<Button>("ConfirmBuyBtn").clicked += OnBuyConfirmed;
        root.Q<Button>("ConfirmSellBtn").clicked += OnSellConfirmed;

        root.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        // Klavye bağlı mı kontrol et ve Yeni Input System ile ESC tuşunu dinle
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // UI görünür durumdaysa kapat
            if (root != null && root.style.display == DisplayStyle.Flex)
            {
                currentLaptop?.ExitLaptop();
            }
        }
    }

    public void OpenUI(LaptopInteractable laptop)
    {
        currentLaptop = laptop;
        root.style.display = DisplayStyle.Flex;
        UpdateBalanceUI();
        SwitchPage(0);
    }

    private void UpdateBalanceUI() => balanceLabel.text = $"{EconomyManager.Instance.currentMoney} TL";

    // --- BİLDİRİM SİSTEMİ (ARTIK DAHA SESLİ) ---
    private void ShowNotify(string msg, Color borderColor)
    {
        if (notifyLabel == null) return;

        notifyLabel.text = msg;
        notifyLabel.style.borderLeftColor = borderColor;
        notifyLabel.style.display = DisplayStyle.Flex;
        notifyLabel.style.opacity = 1; // Görünür kıl

        StopCoroutine("HideNotify");
        StartCoroutine("HideNotify");
    }

    private IEnumerator HideNotify()
    {
        yield return new WaitForSeconds(2.0f);
        float t = 1f;
        while (t > 0)
        {
            t -= Time.deltaTime * 2f;
            notifyLabel.style.opacity = t;
            yield return null;
        }
        notifyLabel.style.display = DisplayStyle.None;
    }

    private void SwitchPage(int index)
    {
        buyPage.style.display = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        cartPage.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
        sellPage.style.display = index == 2 ? DisplayStyle.Flex : DisplayStyle.None;

        buyTab.EnableInClassList("active-sidebar", index == 0);
        cartTab.EnableInClassList("active-sidebar", index == 1);
        sellTab.EnableInClassList("active-sidebar", index == 2);

        if (index == 0) RefreshMarketList();
        if (index == 1) RefreshCartList();
        if (index == 2) RefreshSellList();
    }

    // --- MAĞAZA: SEPETE EKLEME ---
    private void RefreshMarketList()
    {
        marketList.Clear();
        foreach (var item in DeliveryManager.Instance.allAvailableItems)
        {
            marketList.Add(CreateWebCard(item.itemName, item.price, item.itemIcon, "SEPETE EKLE", () => {
                if (cartData.ContainsKey(item.itemID))
                    cartData[item.itemID] = (item, cartData[item.itemID].qty + 1);
                else
                    cartData[item.itemID] = (item, 1);

                ShowNotify($"{item.itemName} sepete eklendi! (Adet: {cartData[item.itemID].qty})", Color.green);
            }));
        }
    }

    // --- SEPET: MİKTARLI LİSTELEME ---
    private void RefreshCartList()
    {
        cartList.Clear();
        int total = 0;
        foreach (var kvp in cartData)
        {
            var item = kvp.Value.data;
            int qty = kvp.Value.qty;
            total += item.price * qty;

            cartList.Add(CreateQtyCard(item.itemName, item.price, item.itemIcon, qty,
                () => { // Eksi butonu
                    if (cartData[kvp.Key].qty > 1)
                        cartData[kvp.Key] = (item, cartData[kvp.Key].qty - 1);
                    else
                        cartData.Remove(kvp.Key);
                    RefreshCartList();
                },
                () => { // Artı butonu
                    cartData[kvp.Key] = (item, cartData[kvp.Key].qty + 1);
                    RefreshCartList();
                }));
        }
        cartTotal.text = $"Toplam: {total} TL";
    }

    // --- SATIŞ: GRUPLU LİSTELEME ---
    private void RefreshSellList()
    {
        sellList.Clear();
        itemsInZoneGrouped.Clear();
        var rawItems = DeliveryManager.Instance.GetItemsInZone();
        foreach (var s in rawItems)
        {
            if (s.itemData == null) continue;
            int id = s.itemData.itemID;
            if (!itemsInZoneGrouped.ContainsKey(id)) itemsInZoneGrouped[id] = new List<SellableItem>();
            itemsInZoneGrouped[id].Add(s);
        }

        int totalGain = 0;
        foreach (var kvp in itemsInZoneGrouped)
        {
            int id = kvp.Key;
            var physicalList = kvp.Value;
            if (!sellQuantities.ContainsKey(id)) sellQuantities[id] = 0;
            sellQuantities[id] = Mathf.Min(sellQuantities[id], physicalList.Count);
            totalGain += sellQuantities[id] * physicalList[0].GetPrice();

            sellList.Add(CreateQtyCard(physicalList[0].itemData.itemName, physicalList[0].GetPrice(), physicalList[0].itemData.itemIcon, sellQuantities[id],
                () => { // Eksi
                    if (sellQuantities[id] > 0) { sellQuantities[id]--; RefreshSellList(); }
                },
                () => { // Artı
                    if (sellQuantities[id] < physicalList.Count) { sellQuantities[id]++; RefreshSellList(); }
                }, physicalList.Count));
        }
        sellTotal.text = $"Kazanç: {totalGain} TL";
    }

    // --- KART OLUŞTURUCU (Miktar Seçicili) ---
    private VisualElement CreateQtyCard(string name, int price, Sprite icon, int currentQty, System.Action onMinus, System.Action onPlus, int? max = null)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("item-card");

        VisualElement img = new VisualElement();
        img.AddToClassList("item-icon");
        if (icon) img.style.backgroundImage = new StyleBackground(icon);

        VisualElement info = new VisualElement();
        info.AddToClassList("item-info-group");

        Label nLabel = new Label(name);
        nLabel.AddToClassList("item-name");

        Label pLabel = new Label($"{price} TL / Adet");
        pLabel.AddToClassList("item-price");

        info.Add(nLabel);
        info.Add(pLabel);

        VisualElement selector = new VisualElement();
        selector.AddToClassList("qty-selector");

        Button mBtn = new Button(onMinus) { text = "-" };

        Label qLbl = new Label(max.HasValue ? $"{currentQty} / {max}" : $"x{currentQty}");
        qLbl.AddToClassList("qty-label");

        Button pBtn = new Button(onPlus) { text = "+" };

        selector.Add(mBtn);
        selector.Add(qLbl);
        selector.Add(pBtn);

        card.Add(img);
        card.Add(info);
        card.Add(selector);

        return card;
    }

    private VisualElement CreateWebCard(string n, int p, Sprite icon, string bTxt, System.Action click)
    {
        VisualElement card = new VisualElement();
        card.AddToClassList("item-card");

        VisualElement img = new VisualElement();
        img.AddToClassList("item-icon");
        if (icon) img.style.backgroundImage = new StyleBackground(icon);

        VisualElement info = new VisualElement();
        info.AddToClassList("item-info-group");

        Label nameLabel = new Label(n);
        nameLabel.AddToClassList("item-name");

        Label priceLabel = new Label(p + " TL");
        priceLabel.AddToClassList("item-price");

        info.Add(nameLabel);
        info.Add(priceLabel);

        Button btn = new Button(click) { text = bTxt };
        btn.AddToClassList("add-button");

        card.Add(img);
        card.Add(info);
        card.Add(btn);

        return card;
    }

    private void OnBuyConfirmed()
    {
        if (cartData.Count == 0) { ShowNotify("Sepetiniz bomboş!", Color.red); return; }
        int total = cartData.Values.Sum(x => x.data.price * x.qty);
        if (EconomyManager.Instance.currentMoney < total) { ShowNotify("Bakiye yetersiz!", Color.red); return; }

        List<int> idsToSend = new List<int>();
        foreach (var kvp in cartData)
            for (int i = 0; i < kvp.Value.qty; i++) idsToSend.Add(kvp.Key);

        DeliveryManager.Instance.PurchaseCartRpc(idsToSend.ToArray(), NetworkManager.Singleton.LocalClientId);
        cartData.Clear();
        ShowNotify("Ödeme başarılı! Kargo hazırlanıyor.", Color.green);
        UpdateBalanceUI();
        RefreshCartList();
    }

    private void OnSellConfirmed()
    {
        List<ulong> idsToSell = new List<ulong>();
        foreach (var kvp in sellQuantities)
            if (kvp.Value > 0)
                for (int i = 0; i < kvp.Value; i++) idsToSell.Add(itemsInZoneGrouped[kvp.Key][i].NetworkObjectId);

        if (idsToSell.Count == 0) { ShowNotify("Hiçbir eşya seçilmedi!", Color.red); return; }

        DeliveryManager.Instance.SellSelectedItemsRpc(idsToSell.ToArray());
        sellQuantities.Clear();
        ShowNotify("Satış tamamlandı!", Color.cyan);
        UpdateBalanceUI();
        RefreshSellList();
    }

    public void CloseUI() => root.style.display = DisplayStyle.None;
}