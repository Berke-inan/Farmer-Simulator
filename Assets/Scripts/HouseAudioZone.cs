using UnityEngine;
using Unity.Netcode;

public class HouseAudioZone : MonoBehaviour
{
    [Header("Yeni Geliþmiþ Ses Yöneticisi")]
    [Tooltip("Dünyadaki DayNightAudioManager objesini buraya sürükle")]
    public DayNightAudioManager audioManager;

    [Header("Ses Ayarlarý")]
    [Tooltip("Dýþarýdayken ses seviyesi (0 ile 1 arasý)")]
    public float outdoorVolume = 1.0f;

    [Tooltip("Evin içindeyken ses seviyesi (örn: 0.2 daha boðuk bir ses verir)")]
    public float indoorVolume = 0.2f;

    [Tooltip("Sesin kýsýlma/açýlma hýzý (Geçiþin yumuþaklýðý)")]
    public float fadeSpeed = 3f;

    private float targetMultiplier;

    private void Start()
    {
        targetMultiplier = outdoorVolume;
    }

    private void Update()
    {
        if (audioManager != null)
        {
            // Hedeflenen çarpaný yumuþakça (Lerp) AudioManager'a gönderiyoruz
            audioManager.houseVolumeMultiplier = Mathf.Lerp(audioManager.houseVolumeMultiplier, targetMultiplier, Time.deltaTime * fadeSpeed);
        }
    }

    // Birisi evin alanýna (Trigger'a) GÝRDÝÐÝNDE:
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out NetworkObject netObj))
        {
            if (netObj.IsPlayerObject && netObj.IsOwner)
            {
                targetMultiplier = indoorVolume;
            }
        }
    }

    // Birisi evin alanýndan (Trigger'dan) ÇIKTIÐINDA:
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out NetworkObject netObj))
        {
            if (netObj.IsPlayerObject && netObj.IsOwner)
            {
                targetMultiplier = outdoorVolume;
            }
        }
    }
}