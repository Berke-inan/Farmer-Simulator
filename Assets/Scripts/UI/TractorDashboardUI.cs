using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(TractorController))]
[RequireComponent(typeof(TractorFuelSystem))]
public class TractorDashboardUI : MonoBehaviour
{
    public UIDocument dashboardDoc; // Traktördeki UI Document buraya sürüklenecek!

    private VisualElement dashboardContainer;
    private Label speedLabel;
    private VisualElement fuelFill;
    private VisualElement conditionFill;

    private TractorController tractor;
    private TractorFuelSystem fuelSystem;

    private void Awake()
    {
        tractor = GetComponent<TractorController>();
        fuelSystem = GetComponent<TractorFuelSystem>();

        if (dashboardDoc != null)
        {
            var root = dashboardDoc.rootVisualElement;
            dashboardContainer = root.Q<VisualElement>("DashboardContainer");
            speedLabel = root.Q<Label>("SpeedLabel");
            fuelFill = root.Q<VisualElement>("FuelFill");
            conditionFill = root.Q<VisualElement>("ConditionFill");

            // Oyun başladığında UI kapalı olsun
            if (dashboardContainer != null)
                dashboardContainer.style.display = DisplayStyle.None;
        }
        else
        {
            Debug.LogError("[Traktör UI] Aga Traktörde UIDocument atanmamış! Inspector'dan scriptin içine sürükle.");
        }
    }

    public void ToggleDashboard(bool show)
    {
        if (dashboardContainer != null)
        {
            dashboardContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void Update()
    {
        // UI sadece açıksa ve yerel oyuncu kullanıyorsa hesaplama yap
        if (dashboardContainer != null && dashboardContainer.style.display == DisplayStyle.Flex && tractor.IsDrivenByMe)
        {
            // 1. Hızı Güncelle
            int currentSpeed = Mathf.RoundToInt(tractor.CurrentSpeedKmh);
            if (speedLabel != null) speedLabel.text = currentSpeed.ToString();

            // 2. Yakıtı Güncelle (Yüzde hesaplama)
            if (fuelSystem != null && fuelFill != null)
            {
                float fuelPercent = (fuelSystem.currentFuel.Value / fuelSystem.maxFuel) * 100f;
                fuelFill.style.width = Length.Percent(fuelPercent);
            }

            // 3. Durum (Eskime) Güncelle
            if (conditionFill != null) conditionFill.style.width = Length.Percent(100f);
        }
    }
}