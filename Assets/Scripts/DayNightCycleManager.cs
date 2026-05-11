using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class DayNightCycleManager : NetworkBehaviour
{
    public static DayNightCycleManager Instance;

    [Header("Time Settings")]
    [Tooltip("Gerçek hayattaki kaç saniye, oyunda 1 tam gün (24 saat) sürsün? Örn: 1200 = 20 dakika")]
    public float realSecondsPerDay = 1200f;

    // Herkes uyuyup sabah olduğunda diğer scriptlerin dinleyebileceği evrensel sinyal
    public static event System.Action YeniGunBasladiSinyali;

    // Ağ üzerinden senkronize edilen saat (0.00 ile 24.00 arası)
    public NetworkVariable<float> currentTime = new NetworkVariable<float>(8f);

    [Header("Visual Settings")]
    public Light sunLight;
    public AnimationCurve sunIntensity = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(5f, 0f),
        new Keyframe(7f, 1f),
        new Keyframe(17f, 1f),
        new Keyframe(19f, 0f),
        new Keyframe(24f, 0f)
    );

    [Header("Ambient Settings")]
    [Tooltip("Gece ve gündüz ortam ışığının şiddetini belirler (0.2 gece için ideal bir loşluktur)")]
    public AnimationCurve ambientIntensityCurve = new AnimationCurve(
        new Keyframe(0f, 0.1f),   // Gece yarısı hafif loş
        new Keyframe(5f, 0.15f),   // Sabaha karşı hala loş
        new Keyframe(7f, 1f),     // Gündüz olunca ortam ışığı tam gücünde (İç mekanlar aydınlanır)
        new Keyframe(17f, 1f),    // Akşam üstüne kadar tam güç
        new Keyframe(19f, 0.15f),  // Akşam olunca tekrar loş gece moduna geçiş
        new Keyframe(24f, 0.1f)
    );



    // Sadece Server'ın bileceği, yatağa yatan oyuncuların listesi
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
        UpdateVisuals();
    }

    private void AdvanceTime()
    {
        float timeMultiplier = 24f / realSecondsPerDay;
        currentTime.Value += Time.deltaTime * timeMultiplier;

        if (currentTime.Value >= 24f)
        {
            currentTime.Value = 0f;
        }
    }

    private void UpdateVisuals()
    {
        if (sunLight != null)
        {
            float sunAngle = (currentTime.Value / 24f) * 360f - 90f;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            sunLight.intensity = sunIntensity.Evaluate(currentTime.Value);
        }

        // YENİ EKLENEN: Ortam ışığının (Global Illumination) saat bazlı güncellenmesi
        RenderSettings.ambientIntensity = ambientIntensityCurve.Evaluate(currentTime.Value);


    }

    // Gece olup olmadığını kontrol eden metot
    public bool IsNight()
    {
        return currentTime.Value >= 19f || currentTime.Value <= 6f;
    }

    // YENİ RPC YAPISI: Sadece Server'a gönderilir, herkes çağırabilir.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SendSleepRequestRpc(ulong clientId)
    {
        if (!IsNight()) return; // Gündüz uyunmaz

        sleepingPlayers.Add(clientId); // Oyuncuyu uyuyanlar listesine ekle

        // Oyundan çıkanlar varsa listeyi temizle
        sleepingPlayers.RemoveWhere(id => !NetworkManager.Singleton.ConnectedClientsIds.Contains(id));

        // Oyundaki toplam oyuncu sayısı
        int totalPlayerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

        Debug.Log($"Uyuyan Oyuncular: {sleepingPlayers.Count} / {totalPlayerCount}");

        // Herkes uyuduysa sabah yap
        if (sleepingPlayers.Count >= totalPlayerCount)
        {
            MakeItMorning();
        }
    }

    private void MakeItMorning()
    {
        currentTime.Value = 6f; // Sabah 6'ya atla
        sleepingPlayers.Clear(); // Uyuyanlar listesini sıfırla
        Debug.Log("Herkes uyudu, sabah oldu!");

        if (YeniGunBasladiSinyali != null)
        {
            YeniGunBasladiSinyali.Invoke();
        }
    }
}