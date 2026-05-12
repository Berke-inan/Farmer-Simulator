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

    private void Awake() => anaGovde = GetComponentInParent<AttachableEquipment>();

    private void OnTriggerStay(Collider other)
    {
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
                        Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
                        bool yakinlardaEkinVar = false;
                        foreach (var col in yakindakiler) { if (col.GetComponent<ModularCrop>()) { yakinlardaEkinVar = true; break; } }

                        if (!yakinlardaEkinVar)
                        {
                            TohumuTopragaBirak(hit.point);
                            islemSayaci = 0f;
                        }
                    }
                }
            }
        }
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
        if (interactor.TryGetComponent(out NetworkedHotbar hotbar) && interactor.TryGetComponent(out InventoryManager inventory))
        {
            int aktifSlotIndex = hotbar.ActiveSlotIndex.Value;
            InventorySlot slot = inventory.Slots[aktifSlotIndex];

            // YENÝ: Eþya türü Tohum (Seed) mi kontrolü
            if (!slot.IsEmpty && slot.Item != null && slot.Item.Type == ItemType.Seed)
            {
                if (mevcutTohum.Value < maxKapasite)
                {
                    MakineyeYukleServerRpc(interactor.NetworkObjectId, aktifSlotIndex);
                }
            }
        }
    }

    [ServerRpc]
    private void MakineyeYukleServerRpc(ulong oyuncuId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuId, out NetworkObject oyuncuNetObj))
        {
            if (oyuncuNetObj.TryGetComponent(out InventoryManager envanter))
            {
                InventorySlot slot = envanter.Slots[slotIndex];
                if (slot.IsEmpty || slot.Item == null || slot.Item.Type != ItemType.Seed) return;

                // YENÝ: Verileri ItemData'dan çekiyoruz
                ItemData data = slot.Item;

                if (mevcutTohum.Value == 0)
                {
                    aktifTohumID = data.TohumID;
                    aktifEkinPrefab = data.EkinPrefab;
                }
                else if (aktifTohumID != data.TohumID) return;

                int bosYer = maxKapasite - mevcutTohum.Value;
                int eklenecekMiktar = Mathf.Min(bosYer, slot.Amount);

                if (eklenecekMiktar > 0)
                {
                    mevcutTohum.Value += eklenecekMiktar;
                    envanter.RemoveItemServerRpc(slotIndex, eklenecekMiktar, transform.position, Vector3.zero, false);
                }
            }
        }
    }
}