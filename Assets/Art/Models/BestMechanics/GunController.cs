using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [Header("Silah Temel Ayarlarý")]
    public float fireRate = 0.1f;
    public float range = 100f;

    [Tooltip("Mermilerin karakterin içinden geçmesi için! (Player katmanýný hariç tutun)")]
    public LayerMask hitLayers = ~0;

    [Header("Görsel ve Ses Efektleri")]
    public ParticleSystem muzzleFlash;
    public AudioClip shootSound;
    public AudioSource audioSource;
    public GameObject bulletHolePrefab;

    [Header("Geri Tepme (Recoil) Sistemi")]
    public Transform weaponVisualModel;
    public Vector3 recoilDirection = new Vector3(0, 0, -1);
    public float kickbackDistance = 0.05f;
    public float recoilRecoverSpeed = 15f;

    private Vector3 originalVisualPosition;
    private float nextFireTime = 0f;
    private Camera mainCam;
    private InputAction shootAction;

    private void Awake()
    {
        shootAction = new InputAction(type: InputActionType.Button, binding: "<Mouse>/leftButton");
        mainCam = Camera.main;

        if (weaponVisualModel != null)
        {
            originalVisualPosition = weaponVisualModel.localPosition;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnEnable() => shootAction.Enable();
    private void OnDisable() => shootAction.Disable();

    private void Update()
    {
        if (shootAction.IsPressed() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (weaponVisualModel != null)
        {
            weaponVisualModel.localPosition = Vector3.Lerp(
                weaponVisualModel.localPosition,
                originalVisualPosition,
                Time.deltaTime * recoilRecoverSpeed
            );
        }
    }

    private void Shoot()
    {
        PlayShootEffectsLocal();

        if (weaponVisualModel != null)
        {
            weaponVisualModel.localPosition += recoilDirection.normalized * kickbackDistance;
        }

        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
        {
            Debug.Log("<color=green>Mermi Çarptý: " + hit.collider.gameObject.name + "</color>");

            SpawnBulletHole(hit);

            // --- AAA GELÝÞMÝÞ ÇARPIÞMA KONTROLÜ ---
            // 1. Önce doðrudan vurulan objede veya ebeveyninde ara
            BreakableTarget target = hit.collider.GetComponentInParent<BreakableTarget>();

            // 2. Bulamazsa, objenin en tepesine (Root) çýk ve tüm alt sülaleyi tara
            if (target == null)
            {
                target = hit.collider.transform.root.GetComponentInChildren<BreakableTarget>();
            }

            // 3. Hedefi bulduysan kýr, bulamadýysan kýrmýzý hata ver
            if (target != null)
            {
                target.TakeDamage();
            }
            else
            {
                Debug.Log("<color=red>HATA: Mermi bardaða çarptý ama bütün hiyerarþiyi taramama raðmen BreakableTarget kodu HÝÇBÝR YERDE bulunamadý!</color>");
            }
        }
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab != null)
        {
            GameObject hole = Instantiate(bulletHolePrefab, hit.point + hit.normal * 0.001f, Quaternion.LookRotation(hit.normal));
            hole.transform.SetParent(hit.collider.transform);
            Destroy(hole, 10f);
        }
    }

    private void PlayShootEffectsLocal()
    {
        if (muzzleFlash != null)
        {
            // Önce durdurup sonra oynatmak, seri atýþlarda partikülün takýlmasýný engeller
            muzzleFlash.Stop();
            muzzleFlash.Play();
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(shootSound);
        }
    }
}