using System.Collections.Generic;
using UnityEngine;

public class NightLightController : MonoBehaviour
{
    [Header("Zamanlama Ayarlarý")]
    [Tooltip("Iþýklarýn yanacaðý oyun saati (Örn: 19.00 için 19)")]
    public float acilmaSaati = 19f;
    [Tooltip("Iþýklarýn kapanacaðý oyun saati (Örn: 06.00 için 6)")]
    public float kapanmaSaati = 6f;

    [Header("Iþýk Gruplarý")]
    [Tooltip("Ýçinde ýþýklar bulunan ana objeleri (Gruplarý) buraya sürükleyin")]
    public List<GameObject> isikGruplari;

    // Iþýklarýn anlýk durumunu takip eder, böylece her saniye SetActive çaðýrmayýz
    private bool isiklarAcikMi = false;

    private void Start()
    {
        // Oyun baþladýðýnda ýþýklarýn doðru durumda olmasý için ilk kontrolü yap
        ZamaniKontrolEt();
    }

    private void Update()
    {
        // DayNightCycleManager henüz yüklenmediyse bekle
        if (DayNightCycleManager.Instance == null) return;

        ZamaniKontrolEt();
    }

    private void ZamaniKontrolEt()
    {
        float suAnkiSaat = DayNightCycleManager.Instance.currentTime.Value;
        bool geceMi;

        // Gece yarýsýný (24.00) geçen bir saat dilimi ayarlandýysa (Örn: 19'da aç, 6'da kapat)
        if (acilmaSaati > kapanmaSaati)
        {
            geceMi = suAnkiSaat >= acilmaSaati || suAnkiSaat < kapanmaSaati;
        }
        // Ayný gün içinde bir saat dilimi ayarlandýysa (Örn: 17'de aç, 23'te kapat)
        else
        {
            geceMi = suAnkiSaat >= acilmaSaati && suAnkiSaat < kapanmaSaati;
        }

        // Eðer saat "Gece" aralýðýndaysa ve ýþýklar KAPALIYSA -> AÇ
        if (geceMi && !isiklarAcikMi)
        {
            IsiklariAcKapat(true);
        }
        // Eðer saat "Gündüz" aralýðýndaysa ve ýþýklar AÇIKSA -> KAPAT
        else if (!geceMi && isiklarAcikMi)
        {
            IsiklariAcKapat(false);
        }
    }

    private void IsiklariAcKapat(bool durum)
    {
        isiklarAcikMi = durum;

        // Listedeki tüm gruplarý tek tek dolaþ ve aç/kapat
        foreach (GameObject grup in isikGruplari)
        {
            if (grup != null)
            {
                grup.SetActive(durum);
            }
        }
    }
}