using UnityEngine;
using Unity.Netcode;

public class BalyaMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;
    private BreakDisableBehavior bozulmaKontrolu;

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
        bozulmaKontrolu = GetComponent<BreakDisableBehavior>();
        if (anaGovde == null)
        {
            Debug.LogError("DÝKKAT: BalyaMakinesi üzerinde AttachableEquipment kodu bulunamadý!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (bozulmaKontrolu != null && bozulmaKontrolu.isBroken.Value) return;
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        if (other.TryGetComponent(out BalyalanabilirObje yerdekiObje))
        {
            if (yerdekiObje.NetworkObject.IsSpawned)
            {
                if (yutulanMiktar.Value == 0)
                {
                    iceridekiMalzemeTipi = yerdekiObje.objeTipi;
                    uretilecekBalyaPrefab = yerdekiObje.balyaPrefab;
                }
                else if (iceridekiMalzemeTipi != yerdekiObje.objeTipi)
                {
                    return;
                }

                yerdekiObje.NetworkObject.Despawn();
                MakineMidesiniDoldurServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MakineMidesiniDoldurServerRpc()
    {
        yutulanMiktar.Value++;

        if (yutulanMiktar.Value >= gerekenMiktar)
        {
            yutulanMiktar.Value = 0;
            BalyaUret();

            iceridekiMalzemeTipi = "";
            uretilecekBalyaPrefab = null;
        }
    }

    private void BalyaUret()
    {
        if (uretilecekBalyaPrefab != null && balyaCikisNoktasi != null)
        {
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