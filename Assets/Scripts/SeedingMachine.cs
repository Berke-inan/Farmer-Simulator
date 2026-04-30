using UnityEngine;
using Unity.Netcode;

public class SeedingMachine : NetworkBehaviour, IInteractable
{
    private AttachableEquipment anaGovde;

    [Header("Kapasite Ayarlarý")]
    public int maxMakineKapasitesi = 50;
    public NetworkVariable<int> mevcutTohumMiktari = new NetworkVariable<int>(0);

    [Header("Ekim Ayarlarý")]
    [Tooltip("Tohumlar arasý minimum mesafe")]
    public float minimumEkimMesafesi = 0.8f;
    public float islemAraligi = 0.2f;
    private float sonIslemZamani;

    // --- YENÝ: MAKÝNE ZEKASI ---
    // Ýçine dökülen tohumun ne olduðunu burada hafýzasýnda tutacak
    private int aktifTohumID = 0;
    private GameObject aktifEkinPrefab;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
    }

    // --- 1. TARLADA ÝLERLERKEN YENÝ LAZER SÝSTEMÝ ÝLE EKÝM ---
    private void Update()
    {
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value || mevcutTohumMiktari.Value <= 0 || aktifEkinPrefab == null) return;

        if (Time.time - sonIslemZamani < islemAraligi) return;

        // Aþaðýya lazer at
        Vector3 lazerBaslangici = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(lazerBaslangici, Vector3.down, out RaycastHit hit, 2f))
        {
            if (hit.collider is TerrainCollider tCol)
            {
                var manager = tCol.GetComponent<TerrainLayerManager>();

                // 1. Þart: Toprak çapalanmýþ mý?
                if (manager != null && manager.IsSoilTilled(hit.point))
                {
                    // 2. Þart: Etrafta baþka bir ekin var mý?
                    Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
                    bool etraftaEkinVar = false;

                    foreach (var c in yakindakiler)
                    {
                        if (c.GetComponent<ModularCrop>()) { etraftaEkinVar = true; break; }
                    }

                    // Her þey uygunsa tohumu topraða býrak
                    if (!etraftaEkinVar)
                    {
                        TohumuEk(hit.point);
                        sonIslemZamani = Time.time;
                    }
                }
            }
        }
    }

    private void TohumuEk(Vector3 nokta)
    {
        mevcutTohumMiktari.Value--;

        // Ekinin topraða gömülmemesi için hafif yukarýdan doðurtuyoruz
        GameObject ekin = Instantiate(aktifEkinPrefab, nokta + (Vector3.up * 0.05f), Quaternion.identity);
        ekin.GetComponent<NetworkObject>().Spawn();

        if (ekin.TryGetComponent(out ModularCrop sc))
        {
            sc.tohumID.Value = aktifTohumID;
        }

        // Tohum bitince hafýzayý temizle (Baþka ürün yüklenebilsin diye)
        if (mevcutTohumMiktari.Value <= 0)
        {
            aktifEkinPrefab = null;
            aktifTohumID = 0;
        }
    }

    // --- 2. OYUNCU ÇUVALDAN MAKÝNEYE TOHUM YÜKLERKEN ---
    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            if (inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out TohumEylemi eldekiTohum))
            {
                if (mevcutTohumMiktari.Value < maxMakineKapasitesi)
                {
                    MakineyeTohumYukleServerRpc(eldekiTohum.NetworkObjectId, interactor.NetworkObjectId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MakineyeTohumYukleServerRpc(ulong tohumObjId, ulong oyuncuId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tohumObjId, out NetworkObject tohumNetObj))
        {
            if (tohumNetObj.TryGetComponent(out TohumEylemi tohumBag))
            {
                // YENÝ: Makine boþsa, içine atýlan çuvalýn tohum bilgisini kopyala
                if (mevcutTohumMiktari.Value == 0)
                {
                    aktifTohumID = tohumBag.tohumID;
                    aktifEkinPrefab = tohumBag.ekinPrefab;
                }
                // Makinede Mýsýr varken oyuncu Buðday atmaya çalýþýyorsa iptal et
                else if (aktifTohumID != tohumBag.tohumID)
                {
                    Debug.LogWarning("Ýþlem Ýptal: Makinede farklý türde bir tohum var!");
                    return;
                }

                int bosYer = maxMakineKapasitesi - mevcutTohumMiktari.Value;
                int eklenecekMiktar = Mathf.Min(bosYer, tohumBag.kalanMiktar.Value);

                if (eklenecekMiktar > 0)
                {
                    mevcutTohumMiktari.Value += eklenecekMiktar;
                    tohumBag.kalanMiktar.Value -= eklenecekMiktar;

                    // Çuval boþaldýysa oyuncunun elinden sil
                    if (tohumBag.kalanMiktar.Value <= 0)
                    {
                        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuId, out NetworkObject oyuncuNetObj))
                        {
                            if (oyuncuNetObj.TryGetComponent(out PlayerInventory envanter))
                            {
                                envanter.EldekiniYokEtServerRpc();
                            }
                        }
                    }
                }
            }
        }
    }
}