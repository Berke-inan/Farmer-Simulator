using UnityEngine;
using Unity.Netcode;

public class BalyaMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;

    [Header("Balya Üretim Ayarlarý")]
    public int gerekenMiktar = 10;
    public NetworkVariable<int> yutulanMiktar = new NetworkVariable<int>(0);

    [Tooltip("Balyanýn doðacaðý yer (Makinenin arkasýnda bir boþ Transform)")]
    public Transform balyaCikisNoktasi;

    private string iceridekiMalzemeTipi = "";
    private GameObject uretilecekBalyaPrefab;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
        if (anaGovde == null)
        {
            Debug.LogError("DÝKKAT: BalyaMakinesi üzerinde AttachableEquipment kodu bulunamadý!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        if (other.TryGetComponent(out BalyalanabilirObje yerdekiObje))
        {
            if (yerdekiObje.NetworkObject.IsSpawned)
            {
                //Makine tamamen boþsa, yuttuðu ilk objenin genetiðini hafýzaya al
                if (yutulanMiktar.Value == 0)
                {
                    iceridekiMalzemeTipi = yerdekiObje.objeTipi;
                    uretilecekBalyaPrefab = yerdekiObje.balyaPrefab;
                }
                //Makine doluysa ama yerdeki obje FARKLI bir tipse yutma!
                else if (iceridekiMalzemeTipi != yerdekiObje.objeTipi)
                {
                    return; // Ýþlemi iptal et, üzerinden geçip gitsin
                }

                //Tip uyuyorsa (veya makine boþsa) objeyi aðdan sil
                yerdekiObje.NetworkObject.Despawn();

                //Mideyi büyüt ve kapasite dolduysa balya fýrlat
                MakineMidesiniDoldurServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MakineMidesiniDoldurServerRpc()
    {
        yutulanMiktar.Value++;

        // Kapasite doldu mu?
        if (yutulanMiktar.Value >= gerekenMiktar)
        {
            yutulanMiktar.Value = 0;
            BalyaUret();

            // Balya çýkýnca makineyi sýfýrla ki sýradaki iþlemde farklý bir ürün yutabilsin
            iceridekiMalzemeTipi = "";
            uretilecekBalyaPrefab = null;
        }
    }

    private void BalyaUret()
    {
        if (uretilecekBalyaPrefab != null && balyaCikisNoktasi != null)
        {
            // Dinamik olarak hafýzadaki prefabi yarat
            GameObject yeniBalya = Instantiate(uretilecekBalyaPrefab, balyaCikisNoktasi.position, balyaCikisNoktasi.rotation);
            yeniBalya.GetComponent<NetworkObject>().Spawn();

            if (yeniBalya.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(-balyaCikisNoktasi.forward * 2f, ForceMode.Impulse);
            }
        }
        else
        {
            Debug.LogWarning("DÝKKAT: Üretilecek Balya Prefab'ý veya Çýkýþ Noktasý eksik!");
        }
    }
}