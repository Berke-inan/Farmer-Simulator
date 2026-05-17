using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem; // YENÝ EKLENDÝ: Modern Input Sistemi Kütüphanesi

public class SulamaEylemi : MonoBehaviour, IUseableTool
{
    [Header("Sulama Ayarlarý")]
    public int fircaBoyutu = 3;
    [Tooltip("Mesafe kontrolü (Kova topraða ne kadar uzaktan su dökebilsin?)")]
    public float sulamaMesafesi = 4f;
    [Tooltip("Basýlý tutarken kaç saniyede bir toprak sulansýn? (0.3 = Saniyede ~3 kere)")]
    public float sulamaAraligi = 0.3f;

    [Header("Enerji Ayarlarý")]
    [Tooltip("Her sulama tetiklendiðinde harcanacak enerji miktarý")]
    public float harcananEnerji = 0.5f;

    [Header("Görsel ve Ses Ayarlarý")]
    public float egilmeAcisi = 45f;
    public float egilmeHizi = 8f;

    public ParticleSystem suPartikulu;
    public AudioSource suSesKaynagi;

    private Quaternion orijinalRotasyon;
    private Quaternion hedefRotasyon;

    private bool isWatering = false;
    private float sonIslemZamani;
    private PlayerInventory cachedInventory;
    private Transform playerCamera;

    private void Awake()
    {
        orijinalRotasyon = transform.localRotation;
        hedefRotasyon = orijinalRotasyon * Quaternion.Euler(egilmeAcisi, 0, 0);

        if (suPartikulu != null) suPartikulu.Stop();
        if (suSesKaynagi != null) { suSesKaynagi.Stop(); suSesKaynagi.loop = true; }
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        cachedInventory = inventory;

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        isWatering = true;
    }

    private void Update()
    {
        // --- ÇÖZÜM BURADA: YENÝ INPUT SÝSTEMÝ MANTIÐI ---
        // Mouse var mý ve sol týk basýlý mý kontrolü (Eski Input.GetMouseButton yerine)
        bool isHoldingClick = Mouse.current != null && Mouse.current.leftButton.isPressed;

        // 1. KONTROL: Eðer sulama modunda deðilsek veya fare sol týký BIRAKILDIYSA sistemi durdur
        if (!isWatering || !isHoldingClick || cachedInventory == null)
        {
            StopWatering();
            return;
        }

        // 2. ENERJÝ KONTROLÜ: Oyuncunun enerjisi bittiyse zorla durdur
        PlayerEnergy enerji = cachedInventory.GetComponent<PlayerEnergy>();
        if (enerji != null && !enerji.EylemYapabilirMi())
        {
            Debug.Log("Çok yorgunsun! Sulama durduruldu.");
            StopWatering();
            return;
        }

        // 3. GÖRSEL VE SESSEL EFEKTLER (Basýlý tutulduðu sürece devrede)
        transform.localRotation = Quaternion.Lerp(transform.localRotation, hedefRotasyon, Time.deltaTime * egilmeHizi);

        if (suPartikulu != null && !suPartikulu.isPlaying) suPartikulu.Play();
        if (suSesKaynagi != null && !suSesKaynagi.isPlaying) suSesKaynagi.Play();

        // 4. SÜREKLÝ SULAMA MATEMATÝÐÝ (Zamanlayýcýya baðlý)
        if (Time.time - sonIslemZamani >= sulamaAraligi)
        {
            HandleContinuousWatering(enerji);
            sonIslemZamani = Time.time;
        }
    }

    private void HandleContinuousWatering(PlayerEnergy enerji)
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, sulamaMesafesi))
        {
            if (hit.collider is TerrainCollider tCol)
            {
                var manager = tCol.GetComponent<TerrainLayerManager>();

                if (manager != null && manager.IsSoilTilled(hit.point))
                {
                    manager.PaintSoilServerRpc(hit.point, manager.wetLayerIndex, fircaBoyutu);

                    if (enerji != null)
                    {
                        enerji.EnerjiHarcaServerRpc(harcananEnerji);
                    }
                }
            }
        }
    }

    private void StopWatering()
    {
        isWatering = false;

        transform.localRotation = Quaternion.Lerp(transform.localRotation, orijinalRotasyon, Time.deltaTime * egilmeHizi);

        if (suPartikulu != null && suPartikulu.isPlaying) suPartikulu.Stop();
        if (suSesKaynagi != null && suSesKaynagi.isPlaying) suSesKaynagi.Stop();
    }
}