using UnityEngine;
using Unity.Netcode;

public class BalerMachine : NetworkBehaviour
{
    // Kendi isWorking deðiþkenini sildik, yerine ana gövdeyi tanýmladýk
    private AttachableEquipment anaGovde;

    [Header("Balya Üretim Ayarlarý")]
    public int gerekenMiktar = 10;
    public NetworkVariable<int> yutulanMiktar = new NetworkVariable<int>(0);

    [Tooltip("Balyanýn doðacaðý yer (Makinenin arkasýnda bir boþ Transform)")]
    public Transform balyaCikisNoktasi;

    // --- DÝNAMÝK HAFIZA SÝSTEMÝ ---
    private string iceridekiMalzemeTipi = "";
    private GameObject uretilecekBalyaPrefab;

    private void Awake()
    {
        // Makinenin ana gövdesini (AttachableEquipment) buluyoruz
        anaGovde = GetComponentInParent<AttachableEquipment>();
        if (anaGovde == null)
        {
            Debug.LogError("DÝKKAT: BalerMachine üzerinde AttachableEquipment kodu bulunamadý!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ýzni kendi isWorking'imizden deðil, anaGovde'den alýyoruz!
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        // Altýmýzdan geçen obje "BalyalanabilirObje" mi?
        if (other.TryGetComponent(out BalyalanabilirObje yerdekiObje))
        {
            if (yerdekiObje.NetworkObject.IsSpawned)
            {
                // 1. DURUM: Makine tamamen boþsa, yuttuðu ilk objenin genetiðini hafýzaya al
                if (yutulanMiktar.Value == 0)
                {
                    iceridekiMalzemeTipi = yerdekiObje.objeTipi;
                    uretilecekBalyaPrefab = yerdekiObje.balyaPrefab;
                }
                // 2. DURUM: Makine doluysa ama yerdeki obje FARKLI bir tipse yutma!
                else if (iceridekiMalzemeTipi != yerdekiObje.objeTipi)
                {
                    return; // Ýþlemi iptal et, üzerinden geçip gitsin
                }

                // 3. Tip uyuyorsa (veya makine boþsa) objeyi aðdan sil (Yut)
                yerdekiObje.NetworkObject.Despawn();

                // 4. Mideyi büyüt ve kapasite dolduysa balya fýrlat
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