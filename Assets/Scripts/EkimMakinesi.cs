using UnityEngine;
using Unity.Netcode;

public class EkimMakinesi : NetworkBehaviour, IInteractable
{
    private AttachableEquipment anaGovde;
    private BreakDisableBehavior bozulmaKontrolu;

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
        bozulmaKontrolu = GetComponent<BreakDisableBehavior>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (bozulmaKontrolu != null && bozulmaKontrolu.isBroken.Value) return;
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value || mevcutTohum.Value <= 0 || aktifEkinPrefab == null) return;

        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        if (other is TerrainCollider tCol)
        {
            Vector3 baslangicNoktasi = transform.position + (Vector3.up * 0.5f);
            if (Physics.Raycast(baslangicNoktasi, Vector3.down, out RaycastHit hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == tCol)
                {
                    TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();
                    if (manager != null && manager.IsSoilTilled(hit.point))
                    {
                        if (!YakinlardaEkinVarMi(hit.point))
                        {
                            TohumuTopragaBirak(hit.point);
                            islemSayaci = 0f;
                        }
                    }
                }
            }
        }
    }

    private bool YakinlardaEkinVarMi(Vector3 nokta)
    {
        Collider[] yakindakiler = Physics.OverlapSphere(nokta, minimumEkimMesafesi);
        foreach (var col in yakindakiler)
        {
            if (col.GetComponent<ModularCrop>()) return true;
        }
        return false;
    }

    private void TohumuTopragaBirak(Vector3 nokta)
    {
        mevcutTohum.Value--;
        GameObject ekin = Instantiate(aktifEkinPrefab, nokta + (Vector3.up * 0.05f), Quaternion.identity);
        ekin.GetComponent<NetworkObject>().Spawn();

        if (ekin.TryGetComponent(out ModularCrop sc)) sc.tohumID.Value = aktifTohumID;

        if (mevcutTohum.Value <= 0) { aktifEkinPrefab = null; aktifTohumID = 0; }
    }

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            int aktifSlotIndex = inventory.activeHotbarIndex.Value;
            InventorySlot slot = inventory.slots[aktifSlotIndex];

            if (!slot.IsEmpty && slot.itemData != null)
            {
                if (mevcutTohum.Value < maxKapasite)
                {
                    MakineyeYukleServerRpc(interactor.NetworkObjectId, aktifSlotIndex);
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MakineyeYukleServerRpc(ulong oyuncuId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuId, out NetworkObject oyuncuNetObj))
        {
            if (oyuncuNetObj.TryGetComponent(out PlayerInventory envanter))
            {
                InventorySlot slot = envanter.slots[slotIndex];
                if (slot.IsEmpty || slot.itemData == null) return;

                ItemData data = slot.itemData;

                if (mevcutTohum.Value == 0)
                {
                    aktifTohumID = data.itemID;
                    aktifEkinPrefab = data.groundPrefab;
                }
                else if (aktifTohumID != data.itemID) return;

                int bosYer = maxKapasite - mevcutTohum.Value;
                int eklenecekMiktar = Mathf.Min(bosYer, slot.amount);

                if (eklenecekMiktar > 0)
                {
                    mevcutTohum.Value += eklenecekMiktar;
                    envanter.DecreaseItemAmountServerRpc(slotIndex, eklenecekMiktar);
                }
            }
        }
    }
}