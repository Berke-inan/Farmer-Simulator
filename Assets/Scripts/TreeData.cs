using UnityEngine;

[CreateAssetMenu(fileName = "YeniAgac", menuName = "Tarim/Agac Verisi")]
public class TreeData : ScriptableObject
{
    [Header("Aðaç Bilgileri")]
    public string agacAdi = "Limon Aðacý";

    [Header("Büyüme Süreleri (Saniye)")]
    [Tooltip("Fideden normal aðaca geçmesi için gereken SÜRE (Örn: 120 saniye = 2 dakika)")]
    public float buyumeSuresi = 120f;

    [Tooltip("Normal aðaçtan meyveye geçmesi için gereken SÜRE (Örn: 180 saniye)")]
    public float meyveVermeSuresi = 180f;

    [Header("Ölüm Ayarlarý (Saniye)")]
    [Tooltip("Aðaç sulandýktan sonra kaç saniye susuz kalýrsa KURUR? (Örn: 300 saniye)")]
    public float kurumaSiniri = 300f;
}