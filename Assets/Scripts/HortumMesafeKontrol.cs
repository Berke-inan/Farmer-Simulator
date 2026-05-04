using Unity.Netcode;
using UnityEngine;

// Bu kodun çalýþmasý için objede kesinlikle PickupableTool olmasý gerektiðini Unity'ye söylüyoruz
[RequireComponent(typeof(PickupableTool))]
public class HortumMesafeKontrol : NetworkBehaviour
{
    [Header("Sýnýr (Hortum) Ayarlarý")]
    public Transform hortumBaslangicNoktasi; // Depodaki sabit baðlantý noktasý
    public float maxUzaklasmaMesafesi = 5f;  // Pompanýn elden düþeceði maksimum mesafe

    private PickupableTool aletKodu;

    private void Awake()
    {
        // Ayný objede bulunan PickupableTool kodunu otomatik olarak bul ve hafýzaya al
        aletKodu = GetComponent<PickupableTool>();
    }

    void Update()
    {
        // 1. Obje aðda oluþmamýþsa çalýþma
        // 2. Alet elde deðilse çalýþma
        // 3. Bu objeyi tutan asýl kiþi (Owner) biz deðilsek çalýþma (Að çakýþmasýný önler)
        if (!IsSpawned || !aletKodu.isEquipped.Value || !IsOwner) return;

        if (hortumBaslangicNoktasi != null)
        {
            // Pompa ile deponun merkezi arasýndaki mesafeyi ölç
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
        // Oyuncunun envanter kodunu bularak, aleti temiz bir þekilde elinden atmasýný saðlýyoruz
        NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);

        if (playerObj != null && playerObj.TryGetComponent(out PlayerInventory inventory))
        {
            inventory.EldekiniYereAt();
        }
        else
        {
            // Eðer envanter bulunamazsa (güvenlik aðý olarak) aletin kendi fýrlatma kodunu çaðýr
            aletKodu.YereFirlat(transform.position, Vector3.down);
        }

        Debug.Log("Hortum çok gerildi, pompa elden düþtü!");
    }
}