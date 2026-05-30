using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class ArsaSecici : MonoBehaviour
{
    public static ArsaSecici Instance;

    [Header("Referanslar")]
    [Tooltip("SAHNEDE BULUNAN yeþil/kýrmýzý küp")]
    public GameObject seciciHologram;
    [Tooltip("KLASÖRDEKÝ (Prefab) mavi kutu")]
    public GameObject seciliKutuPrefab;

    [Header("Renkler")]
    public Color alinabilirRenk = new Color(0, 1, 0, 0.3f);
    public Color alinamazRenk = new Color(1, 0, 0, 0.3f);
    public Color seciliRenk = new Color(0, 0.5f, 1, 0.4f);

    public int arsaBirimFiyati = 1000;

    private Material hologramMateryali;
    private bool secimModuAktif = false;
    public Vector2Int guncelGridHedefi { get; private set; }

    public HashSet<Vector2Int> sepettekiArsalar = new HashSet<Vector2Int>();
    private Dictionary<Vector2Int, GameObject> sepettekiGorseller = new Dictionary<Vector2Int, GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (seciciHologram != null)
        {
            // Renderer alt objede de olsa bulmasýný saðladýk
            Renderer rend = seciciHologram.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                hologramMateryali = rend.material;
            }
            seciciHologram.SetActive(false);
        }
    }

    private void Update()
    {
        if (!secimModuAktif || seciciHologram == null || Camera.main == null || hologramMateryali == null) return;

        SeciciyiGuncelle();

        if (Mouse.current.leftButton.wasPressedThisFrame && seciciHologram.activeSelf)
        {
            SepetiYonet();
        }
    }

    private void SeciciyiGuncelle()
    {
        Vector2 farePozisyonu = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(farePozisyonu);
        RaycastHit[] hits = Physics.RaycastAll(ray, 500f);

        if (hits.Length > 0)
        {
            RaycastHit enAltZemin = hits[0];
            for (int i = 1; i < hits.Length; i++)
            {
                if (hits[i].distance > enAltZemin.distance) { enAltZemin = hits[i]; }
            }

            if (!seciciHologram.activeSelf) seciciHologram.SetActive(true);

            guncelGridHedefi = ArsaManager.Instance.PozisyonuGridKoordinatinaCevir(enAltZemin.point);
            Vector3 snapPozisyonu = ArsaManager.Instance.GridKoordinatiniPozisyonaCevir(guncelGridHedefi);

            seciciHologram.transform.position = new Vector3(snapPozisyonu.x, enAltZemin.point.y, snapPozisyonu.z);

            if (ArsaManager.Instance.ArsaBizimMi(guncelGridHedefi))
                hologramMateryali.color = alinamazRenk;
            else if (sepettekiArsalar.Contains(guncelGridHedefi))
                hologramMateryali.color = seciliRenk;
            else
                hologramMateryali.color = alinabilirRenk;
        }
        else
        {
            seciciHologram.SetActive(false);
        }
    }

    private void SepetiYonet()
    {
        if (ArsaManager.Instance.ArsaBizimMi(guncelGridHedefi)) return;

        if (sepettekiArsalar.Contains(guncelGridHedefi))
        {
            sepettekiArsalar.Remove(guncelGridHedefi);
            if (sepettekiGorseller.ContainsKey(guncelGridHedefi))
            {
                Destroy(sepettekiGorseller[guncelGridHedefi]);
                sepettekiGorseller.Remove(guncelGridHedefi);
            }
        }
        else
        {
            sepettekiArsalar.Add(guncelGridHedefi);
            Vector3 pos = ArsaManager.Instance.GridKoordinatiniPozisyonaCevir(guncelGridHedefi);
            pos.y = seciciHologram.transform.position.y;

            GameObject isaret = Instantiate(seciliKutuPrefab, pos, Quaternion.identity);
            sepettekiGorseller.Add(guncelGridHedefi, isaret);
        }
    }

    public void UI_SonSecileniIptalEt()
    {
        if (sepettekiArsalar.Count > 0)
        {
            Vector2Int sonEklenen = sepettekiArsalar.Last();
            sepettekiArsalar.Remove(sonEklenen);
            if (sepettekiGorseller.ContainsKey(sonEklenen))
            {
                Destroy(sepettekiGorseller[sonEklenen]);
                sepettekiGorseller.Remove(sonEklenen);
            }
        }
    }

    public void UI_SatinAlimiTamamla()
    {
        foreach (var arsa in sepettekiArsalar)
        {
            Vector3 dunyaPos = ArsaManager.Instance.GridKoordinatiniPozisyonaCevir(arsa);
            ArsaManager.Instance.ArsaSatinAlServerRpc(dunyaPos);
        }
        SepetiTemizle();
    }

    public void SecimModunuAc()
    {
        secimModuAktif = true;
    }

    public void SecimModunuKapat()
    {
        secimModuAktif = false;
        if (seciciHologram != null) seciciHologram.SetActive(false);
    }

    public void SepetiTemizle()
    {
        sepettekiArsalar.Clear();
        foreach (var gorsel in sepettekiGorseller.Values)
        {
            if (gorsel != null) Destroy(gorsel);
        }
        sepettekiGorseller.Clear();
    }
}