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
    [Tooltip("Gece dünyayı gerçekten karartan ayar budur.")]
    public AnimationCurve ambientIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.02f),   // Gece yarısı zifiri (0'a yakın)
        new Keyframe(6f, 0.05f),   // Şafak öncesi loşluk
        new Keyframe(7.5f, 1.0f),  // Gündüz tam aydınlık
        new Keyframe(17f, 1.0f),   // Akşam üstüne kadar parlak
        new Keyframe(18.5f, 0.05f),// Gün batımı sonrası hızlı kararma
        new Keyframe(24f, 0.02f)
    );

    [Header("Yansıma Şiddeti (Parlama Sorunu Çözümü)")]
    [Tooltip("Gece yerlerin parlamasını engelleyen kritik eğri.")]
    public AnimationCurve reflectionIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.01f),   // Gece yansıma kapalı (Yer parlamaz)
        new Keyframe(6f, 0.01f),
        new Keyframe(8f, 1.0f),    // Gündüz yansıma açık
        new Keyframe(16.5f, 1.0f),
        new Keyframe(18.5f, 0.01f),
        new Keyframe(24f, 0.01f)
    );

    private HashSet<ulong> sleepingPlayers = new HashSet<ulong>();

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

        // Görseller tüm oyuncuların bilgisayarında güncellenir
        UpdateVisuals();
    }

    private void AdvanceTime()
    {
        float timeMultiplier = 24f / realSecondsPerDay;
        currentTime.Value += Time.deltaTime * timeMultiplier;
        if (currentTime.Value >= 24f) currentTime.Value = 0f;
    }

    private void UpdateVisuals()
    {
        float t = currentTime.Value;
        float sunAngle = (t / 24f) * 360f - 90f;

        // 1. Güneş ve Ay Işıkları
        if (sunLight != null)
        {
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sunLight.intensity = sunIntensity.Evaluate(t);
        }

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0f);
            // Ay sadece gece ufkun üzerindeyse loş bir ışık verir
            float moonHeight = Mathf.Clamp01(-moonLight.transform.forward.y);
            moonLight.intensity = moonHeight * 0.15f;
        }

        // 2. Yerlerin Parlamasını Engelleyen Kritik Ayarlar
        // Ortam ışığını (Ambient) ve Gökyüzü yansımasını (Reflection) karartıyoruz
        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(t);
        RenderSettings.reflectionIntensity = reflectionIntensityCurve.Evaluate(t);

        // 3. Ultra Gerçekçi Skybox Senkronizasyonu
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
        sleepingPlayers.Clear();
        YeniGunBasladiSinyali?.Invoke();
    }
}