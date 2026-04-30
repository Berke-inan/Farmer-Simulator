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

    // --- DÝNAMÝK HAFIZA SÝSTEMÝ ---
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

    // --- SENÝN KUSURSUZ ÇALIÞAN MANTIÐIN: OnTriggerEnter ---
    private void OnTriggerEnter(Collider other)
    {
        // 1. Ýzinleri Kontrol Et (Sadece sunucu ve makine açýksa çalýþýr)
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        // 2. Altýmýzdan geçen obje "BalyalanabilirObje" mi?
        if (other.TryGetComponent(out BalyalanabilirObje yerdekiObje))
        {
            if (yerdekiObje.NetworkObject.IsSpawned)
            {
                // DURUM 1: Makine tamamen boþsa, yuttuðu ilk objenin genetiðini hafýzaya al
                if (yutulanMiktar.Value == 0)
                {
                    iceridekiMalzemeTipi = yerdekiObje.objeTipi;
                    uretilecekBalyaPrefab = yerdekiObje.balyaPrefab;
                }
                // DURUM 2: Makine doluysa ama yerdeki obje FARKLI bir tipse yutma!
                else if (iceridekiMalzemeTipi != yerdekiObje.objeTipi)
                {
                    return; // Ýþlemi iptal et, üzerinden geçip gitsin
                }

                // DURUM 3: Tip uyuyorsa (veya makine boþsa) objeyi aðdan sil (Yut)
                yerdekiObje.NetworkObject.Despawn();

                // DURUM 4: Mideyi büyüt ve kapasite dolduysa balya fýrlat
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