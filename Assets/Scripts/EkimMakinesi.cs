using UnityEngine;
using Unity.Netcode;

public class EkimMakinesi : NetworkBehaviour, IInteractable
{
    private AttachableEquipment anaGovde;

    [Header("Makine Kapasitesi")]
    public int maxKapasite = 50;
    public NetworkVariable<int> mevcutTohum = new NetworkVariable<int>(0);

    private int aktifTohumID = 0;
    private GameObject aktifEkinPrefab;

    [Header("Ekim Ayarlarý")]
    public float minimumEkimMesafesi = 0.8f;
    public float islemAraligi = 0.15f;
    private float islemSayaci = 0f;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        if (anaGovde == null || !anaGovde.isWorking.Value) return;

        if (mevcutTohum.Value <= 0) return;
        if (aktifEkinPrefab == null)
        {
            Debug.LogWarning("EKÝM HATA: Tohum yüklü ama 'Ekin Prefab' bilgisi boþ gelmiþ! Çuval objesini kontrol et.");
            return;
        }

        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        if (other is TerrainCollider tCol)
        {
            // Debug.Log("Sensör topraða deðiyor, lazer atýlýyor...");

            Vector3 baslangicNoktasi = transform.position + (Vector3.up * 0.5f);

            if (Physics.Raycast(baslangicNoktasi, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == tCol)
                {
                    TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();
                    if (manager != null)
                    {
                        bool isTilled = manager.IsSoilTilled(hit.point);
                        Debug.Log("EKÝM ADIM 2: Lazer topraðý buldu. Burasý çapalanmýþ mý? -> " + isTilled);

                        if (isTilled)
                        {
                            Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
                            bool yakinlardaEkinVar = false;

                            foreach (var col in yakindakiler)
                            {
                                if (col.GetComponent<ModularCrop>()) { yakinlardaEkinVar = true; break; }
                            }

                            Debug.Log("EKÝM ADIM 3: Etrafta baþka ekin var mý? -> " + yakinlardaEkinVar);

                            if (!yakinlardaEkinVar)
                            {
                                Debug.Log("EKÝM ADIM 4: HER ÞEY MÜKEMMEL! Tohum ekiliyor...");
                                TohumuTopragaBirak(hit.point);
                                islemSayaci = 0f;
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("EKÝM HATA: Lazer yere ulaþamadý!");
            }
        }
    }

    private void TohumuTopragaBirak(Vector3 nokta)
    {
        mevcutTohum.Value--;
        GameObject ekin = Instantiate(aktifEkinPrefab, nokta + (Vector3.up * 0.05f), Quaternion.identity);
        ekin.GetComponent<NetworkObject>().Spawn();

        if (ekin.TryGetComponent(out ModularCrop sc))
        {
            sc.tohumID.Value = aktifTohumID;
        }

        if (mevcutTohum.Value <= 0)
        {
            aktifEkinPrefab = null;
            aktifTohumID = 0;
        }
    }

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            if (inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out TohumEylemi eldekiTohum))
            {
                if (mevcutTohum.Value < maxKapasite)
                {
                    MakineyeYukleServerRpc(eldekiTohum.NetworkObjectId, interactor.NetworkObjectId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void MakineyeYukleServerRpc(ulong tohumObjId, ulong oyuncuId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tohumObjId, out NetworkObject tohumNetObj))
        {
            if (tohumNetObj.TryGetComponent(out TohumEylemi tohumBag))
            {
                if (mevcutTohum.Value == 0)
                {
                    aktifTohumID = tohumBag.tohumID;
                    aktifEkinPrefab = tohumBag.ekinPrefab;
                }
                else if (aktifTohumID != tohumBag.tohumID)
                {
                    return;
                }

                int bosYer = maxKapasite - mevcutTohum.Value;
                int eklenecekMiktar = Mathf.Min(bosYer, tohumBag.kalanMiktar.Value);

                if (eklenecekMiktar > 0)
                {
                    mevcutTohum.Value += eklenecekMiktar;
                    tohumBag.kalanMiktar.Value -= eklenecekMiktar;

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