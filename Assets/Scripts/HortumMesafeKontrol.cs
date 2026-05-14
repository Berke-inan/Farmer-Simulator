using UnityEngine;
using Unity.Netcode;

public class HortumMesafeKontrol : MonoBehaviour
{
    [Header("Sýnýr (Hortum) Ayarlarý")]
    public Transform hortumBaslangicNoktasi; // Depodaki sabit baðlantý noktasý
    public float maxUzaklasmaMesafesi = 5f;  // Pompanýn elden düþeceði maksimum mesafe

    private PlayerInventory inventory;

    private void Start()
    {
        // Bu obje 'handTransform' altýna Instantiate edildiði için
        // hiyerarþide yukarý çýkarak PlayerInventory kodunu buluyoruz.
        inventory = GetComponentInParent<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogError("HortumMesafeKontrol: PlayerInventory bulunamadý! Obje oyuncunun elinde mi?");
            enabled = false;
        }
    }

    void Update()
    {
        // Sadece bu oyuncunun sahibi bizsek (Local Player) mesafe kontrolü yapalým
        if (inventory == null || !inventory.IsOwner) return;

        if (hortumBaslangicNoktasi != null)
        {
            // Pompa ile depo arasýndaki mesafeyi ölçüyoruz
            float mesafe = Vector3.Distance(transform.position, hortumBaslangicNoktasi.position);

            // Eðer mesafe sýnýrý aþarsa zorla yere at
            if (mesafe > maxUzaklasmaMesafesi)
            {
                ZorlaYereBirak();
            }
        }
    }

    private void ZorlaYereBirak()
    {
        Debug.Log("Hortum çok gerildi, pompa elden düþtü!");

        // PlayerInventory içindeki mevcut yere atma mantýðýný tetikliyoruz
        inventory.EldekiniYereAt();
    }
}