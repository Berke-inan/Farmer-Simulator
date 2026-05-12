using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PlayerCrosshair : NetworkBehaviour
{
    private UIDocument uiDocument;
    private VisualElement crosshairDot;

    public override void OnNetworkSpawn()
    {
        // Bizim karakterimiz deðilse arayüzü tamamen kapat
        if (!IsOwner)
        {
            GetComponent<UIDocument>().enabled = false;
            enabled = false;
            return;
        }

        uiDocument = GetComponent<UIDocument>();

        var root = uiDocument.rootVisualElement;
        if (root != null)
        {
            crosshairDot = root.Q<VisualElement>("CrosshairDot");
        }
    }

    private void Update()
    {
        if (crosshairDot == null) return;

        // --- ÝÞTE O ZEKÝ VE BAÐIMSIZ MANTIK ---
        // 1. Karakterin bir Parent'ý (Ebeveyni) var mý?
        // 2. Varsa, bu ebeveyn bir TractorController mý?
        bool isDrivingTractor = transform.parent != null && transform.parent.GetComponent<TractorController>() != null;

        // Fare menüdeyse VEYA traktörün içindeysek GÝZLE
        if (UnityEngine.Cursor.visible || isDrivingTractor)
        {
            crosshairDot.style.display = DisplayStyle.None;
        }
        else // Dýþarýdaysak ve yürüyorsak GÖSTER
        {
            crosshairDot.style.display = DisplayStyle.Flex;
        }
    }
}