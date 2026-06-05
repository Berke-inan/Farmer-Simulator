using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(TractorController))]
[RequireComponent(typeof(TractorFuelSystem))]
public class TractorDashboardUI : MonoBehaviour
{
    public UIDocument dashboardDoc;

    private VisualElement dashboardContainer;
    private Label speedLabel;
    private VisualElement fuelFill;
    private VisualElement conditionFill;

    private TractorController tractor;
    private TractorFuelSystem fuelSystem;
    private VehicleStatus vehicleStatus;

    private void Awake()
    {
        tractor = GetComponent<TractorController>();
        fuelSystem = GetComponent<TractorFuelSystem>();
        vehicleStatus = GetComponent<VehicleStatus>();
    }

    private void Start()
    {
        if (dashboardDoc == null)
        {
            Debug.LogError("HATA 1: 'dashboardDoc' BOŞ! Inspector üzerinden UIDocument bileşenini bu scripte sürükleyip bırakmayı unutmuşsun.");
            return;
        }

        if (dashboardDoc.rootVisualElement != null)
        {
            var root = dashboardDoc.rootVisualElement;
            dashboardContainer = root.Q<VisualElement>("DashboardContainer");
            speedLabel = root.Q<Label>("SpeedLabel");
            fuelFill = root.Q<VisualElement>("FuelFill");
            conditionFill = root.Q<VisualElement>("ConditionFill");

            if (dashboardContainer != null)
            {
                dashboardContainer.style.display = DisplayStyle.None;
                Debug.Log("BAŞARILI: Traktör UI öğeleri başarıyla bulundu ve başlangıçta gizlendi.");
            }
            else
            {
                Debug.LogError("HATA 2: 'DashboardContainer' isimli öğe UXML içinde bulunamadı!");
            }
        }
    }

    public void ToggleDashboard(bool show)
    {
        Debug.Log($"ToggleDashboard Çalıştı! Arayüz Açılma İsteği: {show}");

        if (dashboardContainer != null)
        {
            dashboardContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }
        else
        {
            Debug.LogError("HATA 3: UI açılamadı çünkü dashboardContainer referansı kayıp!");
        }
    }

    private void Update()
    {
        if (dashboardContainer != null && dashboardContainer.style.display == DisplayStyle.Flex && tractor.IsDrivenByMe)
        {
            int currentSpeed = Mathf.RoundToInt(tractor.CurrentSpeedKmh);
            if (speedLabel != null) speedLabel.text = currentSpeed.ToString();

            if (fuelSystem != null && fuelFill != null)
            {
                float fuelPercent = (fuelSystem.currentFuel.Value / fuelSystem.maxFuel) * 100f;
                fuelFill.style.width = Length.Percent(fuelPercent);
            }

            if (conditionFill != null)
            {
                float conditionPercent = 100f;
                if (vehicleStatus != null)
                {
                    conditionPercent = vehicleStatus.TraktorDurumYuzdesi;
                }
                conditionFill.style.width = Length.Percent(conditionPercent);
            }
        }
    }
}