using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DayNightCycleManager : NetworkBehaviour
{
    public static DayNightCycleManager Instance;

    [Header("Zaman Ayarları")]
    [Tooltip("Gerçek hayattaki kaç saniye, oyunda 24 saat sürsün?")]
    public float realSecondsPerDay = 1200f;
    public NetworkVariable<float> currentTime = new NetworkVariable<float>(8f);

    public static event System.Action YeniGunBasladiSinyali;

    [Header("Işık Kaynakları")]
    public Light sunLight;
    public Light moonLight;

    [Header("Güneş Şiddeti (Gündüz 1.2, Gece 0)")]
    public AnimationCurve sunIntensity = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(5.5f, 0f),
        new Keyframe(7f, 1.2f),
        new Keyframe(17.5f, 1.2f),
        new Keyframe(19f, 0f),
        new Keyframe(24f, 0f)
    );

    [Header("Ortam Işığı (Yerlerin Kararması İçin)")]
    public AnimationCurve ambientIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.15f), // GECE AYARI: 0.02'den 0.15'e çıkarıldı
        new Keyframe(6f, 0.2f),  // SABAHA KARŞI: 0.05'ten 0.2'ye çıkarıldı
        new Keyframe(7.5f, 1.0f),
        new Keyframe(17f, 1.0f),
        new Keyframe(18.5f, 0.2f), // AKŞAM ÜSTÜ: 0.05'ten 0.2'ye çıkarıldı
        new Keyframe(24f, 0.15f) // GECE AYARI: 0.02'den 0.15'e çıkarıldı
    );

    [Header("Yansıma Şiddeti (Parlama Sorunu Çözümü)")]
    public AnimationCurve reflectionIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.05f), // GECE YANSIMASI: 0.01'den 0.05'e çıkarıldı
        new Keyframe(6f, 0.05f), // SABAHA KARŞI YANSIMA: 0.01'den 0.05'e çıkarıldı
        new Keyframe(8f, 1.0f),
        new Keyframe(16.5f, 1.0f),
        new Keyframe(18.5f, 0.05f), // AKŞAM YANSIMASI: 0.01'den 0.05'e çıkarıldı
        new Keyframe(24f, 0.05f) // GECE YANSIMASI: 0.01'den 0.05'e çıkarıldı
    );

    private HashSet<ulong> sleepingPlayers = new HashSet<ulong>();
    private bool morningTriggered = false; // Doğal sabahı yakalamak için kilit

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (IsServer)
        {
            AdvanceTime();
        }

        UpdateVisuals();
    }

    private void AdvanceTime()
    {
        float timeMultiplier = 24f / realSecondsPerDay;
        currentTime.Value += Time.deltaTime * timeMultiplier;

        // Doğal yollarla sabah saat 6'yı geçtiğinde sinyali tetikle
        if (currentTime.Value >= 6f && currentTime.Value < 7f && !morningTriggered)
        {
            morningTriggered = true;
            YeniGunBasladiSinyali?.Invoke();
            Debug.Log("Doğal yollarla sabah oldu, yeni gün sinyali gönderildi.");
        }

        if (currentTime.Value >= 24f)
        {
            currentTime.Value = 0f;
            morningTriggered = false; // Yeni gece yarısı olduğunda kilidi sıfırla
        }
    }

    private void UpdateVisuals()
    {
        float t = currentTime.Value;
        float sunAngle = (t / 24f) * 360f - 90f;

        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sunLight.intensity = sunIntensity.Evaluate(t);
        }

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
            float moonHeight = Mathf.Clamp01(-moonLight.transform.forward.y);
            // AY IŞIĞI ŞİDDETİ: 0.15f'den 0.35f'e çıkarıldı (Gece aydınlatmasını sağlar)
            moonLight.intensity = moonHeight * 0.35f;
        }

        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(t);
        RenderSettings.reflectionIntensity = reflectionIntensityCurve.Evaluate(t);

        if (RenderSettings.skybox != null)
        {
            if (sunLight != null)
                RenderSettings.skybox.SetVector("_SunDir", -sunLight.transform.forward);

            if (moonLight != null)
                RenderSettings.skybox.SetVector("_MoonDir", -moonLight.transform.forward);
        }
    }

    public bool IsNight() => currentTime.Value >= 19f || currentTime.Value <= 6f;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SendSleepRequestRpc(ulong clientId)
    {
        if (!IsNight()) return;
        sleepingPlayers.Add(clientId);
        int totalPlayerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (sleepingPlayers.Count >= totalPlayerCount) MakeItMorning();
    }

    private void MakeItMorning()
    {
        currentTime.Value = 6.5f;
        morningTriggered = true; // Oyuncular uyuyarak sabahı getirdi, kilidi kapat
        sleepingPlayers.Clear();
        YeniGunBasladiSinyali?.Invoke();
        Debug.Log("Herkes uyudu, yeni gün sinyali gönderildi.");
    }
}