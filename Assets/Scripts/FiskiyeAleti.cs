using UnityEngine;
using Unity.Netcode;

public class FiskiyeAleti : MonoBehaviour, IUseableTool
{
    [Header("Alan Göstergesi")]
    [Tooltip("Senin hazýrladýðýn hologram objesi")]
    public GameObject sulamaAlaniGostergesi;

    [Header("Mesafe Ayarlarý")]
    [Tooltip("Fýskiyeyi en fazla kaç metre uzaða koyabilesin? (Yatay Sýnýr)")]
    public float yerlestirmeMesafesi = 4f;
    [Tooltip("Hologramý en fazla ne kadar uzakta görebilesin?")]
    public float hologramGorusMesafesi = 20f;

    // --- MUCÝZE FONKSÝYON: Iþýn her zaman uzaða gider (Hipotenüs sorunu yaþanmaz) ---
    private bool TopragiBul(out RaycastHit gercekToprakHit)
    {
        gercekToprakHit = default;
        if (Camera.main == null) return false;

        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

        // Iþýný yerleþtirme mesafesiyle deðil, her zaman görüþ mesafesiyle (20m) atýyoruz
        RaycastHit[] hits = Physics.RaycastAll(ray, hologramGorusMesafesi);

        foreach (RaycastHit h in hits)
        {
            if (h.collider is TerrainCollider)
            {
                gercekToprakHit = h;
                return true;
            }
        }
        return false;
    }

    private void Update()
    {
        if (sulamaAlaniGostergesi != null)
        {
            if (TopragiBul(out RaycastHit hit))
            {
                if (hit.normal.y > 0.5f) // Sadece düz zeminde göster
                {
                    sulamaAlaniGostergesi.SetActive(true);

                    // Hologramý tam topraðýn üstüne koy ve yere paralel yapýþtýr
                    sulamaAlaniGostergesi.transform.position = hit.point + (Vector3.up * 0.05f);
                    sulamaAlaniGostergesi.transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                else
                {
                    sulamaAlaniGostergesi.SetActive(false);
                }
            }
            else
            {
                sulamaAlaniGostergesi.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        if (sulamaAlaniGostergesi != null) sulamaAlaniGostergesi.SetActive(false);
    }

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        // Önce ýþýnýmýzýn topraðý bulup bulmadýðýna bakýyoruz
        if (TopragiBul(out RaycastHit gercekToprakHit))
        {
            // --- YATAY MESAFE HESAPLAMASI ---
            // Yükseklikleri (Y eksenini) yoksayarak, oyuncunun ayaklarý ile hedefin tam uzaklýðýný buluyoruz.
            Vector3 oyuncuYatayPos = new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z);
            Vector3 hedefYatayPos = new Vector3(gercekToprakHit.point.x, 0, gercekToprakHit.point.z);

            float gercekUzaklik = Vector3.Distance(oyuncuYatayPos, hedefYatayPos);

            // Eðer kuþ uçuþu yatay uzaklýk 4 metreden kýsaysa yerleþtirmeye izin ver
            if (gercekUzaklik <= yerlestirmeMesafesi)
            {
                if (gercekToprakHit.normal.y > 0.5f)
                {
                    int activeIndex = inventory.activeHotbarIndex.Value;
                    inventory.FiskiyeYerlestirServerRpc(gercekToprakHit.point, activeIndex);
                }
            }
            else
            {
                // Uzaksa reddet
                Debug.Log("Fýskiyeyi oraya yerleþtirmek için çok uzaksýn! Biraz yaklaþ.");
            }
        }
    }
}