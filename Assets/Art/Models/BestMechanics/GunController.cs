using UnityEngine;
using UnityEngine.InputSystem;

public class GunController : MonoBehaviour
{
    [Header("Silah Temel Ayarlarý")]
    public float fireRate = 0.1f;
    public float range = 100f;
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

    [Header("Fiziksel Etki (Vuruþ Hissiyatý)")]
    public float mermiItmeGucu = 500f; // Vurulan objelere uygulanacak güç

    private Vector3 originalVisualPosition;
    private float nextFireTime = 0f;
    private Camera mainCam;
    private InputAction shootAction;

    [Header("Ses ve Etkileþim")]
    public float silahSesiMenzili = 30f;

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

            // --- AAA FÝZÝKSEL GERÝBÝLDÝRÝM ---
            // Vurulan objenin bir fiziði (Rigidbody) varsa, merminin yönünde güç uygula
            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForceAtPosition(mainCam.transform.forward * mermiItmeGucu, hit.point);
            }

            // --- GELÝÞMÝÞ KIRILMA KONTROLÜ ---
            BreakableTarget target = hit.collider.GetComponentInParent<BreakableTarget>();

            if (target == null)
            {
                target = hit.collider.transform.root.GetComponentInChildren<BreakableTarget>();
            }

            if (target != null)
            {
                target.TakeDamage();
            }
        }
        // ...
        Collider[] duyulanlar = Physics.OverlapSphere(transform.position, silahSesiMenzili);
        Debug.Log($"<color=orange>Silah patladý! Etrafta {duyulanlar.Length} adet obje duydu.</color>"); // EKLENEN LOG

        foreach (Collider col in duyulanlar)
        {
            HorseController at = col.GetComponentInParent<HorseController>();
            if (at != null)
            {
                Debug.Log("<color=red>At sesi duydu ve korkutma komutu gönderiliyor!</color>"); // EKLENEN LOG
                at.KorkutServerRpc(transform.position);
            }
        }
    }

    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab != null)
        {
            // YÖN DÜZELTMESÝ: -hit.normal sayesinde URP Decal yüzeye tam olarak bakar
            GameObject hole = Instantiate(bulletHolePrefab, hit.point, Quaternion.LookRotation(-hit.normal));
            hole.transform.SetParent(hit.collider.transform);
            Destroy(hole, 10f);
        }
    }

    private void PlayShootEffectsLocal()
    {
        if (muzzleFlash != null)
        {
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