using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

                // 1. KORUMA: Elimizdeki eþyanýn "ekinPrefab"ý yoksa o tohum deðildir, makineye alma!
                if (data.ekinPrefab == null) return;

                if (mevcutTohum.Value == 0)
                {
                    aktifTohumID = data.itemID;
                    // HATA BURADAYDI: groundPrefab yerine ekinPrefab olmalý
                    aktifEkinPrefab = data.ekinPrefab;
                }
                else if (aktifTohumID != data.itemID) return; // Farklý tohum yüklenmesini engeller

                int bosYer = maxKapasite - mevcutTohum.Value;

                // YENÝ SÝSTEM ENTEGRASYONU: Paketin içindeki gerçek tohum sayýsýný bul
                int pakettekiTohumSayisi = slot.kalanEkimHakki == -1 ? data.maxEkimHakki : slot.kalanEkimHakki;

                // Eðer makinede paketin tamamýný alacak yer varsa
                if (bosYer >= pakettekiTohumSayisi)
                {
                    mevcutTohum.Value += pakettekiTohumSayisi;
                    // Paketi envanterden sil (1 adet paketi eksilt)
                    envanter.DecreaseItemAmountServerRpc(slotIndex, 1);
                }
                else
                {
                    Debug.Log("Makinede tam bir paket tohum için yeterli yer yok!");
                }
            }
        }
    }

    public List<ActionPrompt> GetPrompts()
    {
        List<ActionPrompt> prompts = new List<ActionPrompt>();
        prompts.Add(new ActionPrompt("V", "Ekim Yap (Aç/Kapat)"));

        // Römorktaki gibi dinamik "E" tuþu ipuçlarýný ekliyoruz
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
                InventorySlot aktifSlot = inventory.slots[inventory.activeHotbarIndex.Value];

                // Elimizde bir tohum paketi varsa (ekinPrefab doluysa tohumdur)
                if (!aktifSlot.IsEmpty && aktifSlot.itemData != null && aktifSlot.itemData.ekinPrefab != null)
                {
                    if (mevcutTohum.Value < maxKapasite)
                        prompts.Add(new ActionPrompt("E", "Tohum Yükle"));
                    else
                        prompts.Add(new ActionPrompt("E", "Makine Dolu"));
                }
            }
        }

        return prompts;
    }
}