using UnityEngine;
using Unity.Netcode;

public class BicerdoverMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
        if (anaGovde == null) Debug.LogError("DÝKKAT: BicerdoverMakinesi kodu, AttachableEquipment ile ayný veya alt objede olmalý!");
    }

    // ÝÞTE SENÝN EKLEDÝÐÝN O "IS TRIGGER" ÝÞARETLÝ BOX COLLIDER BURAYI TETÝKLER!
    private void OnTriggerStay(Collider other)
    {
        // Sadece server'da çalýþsýn ve makine 'V' ile çalýþtýrýlmýþsa iþlem yapsýn
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        // Yeþil sensörün içine giren obje bir ekin mi?
        if (other.TryGetComponent(out ModularCrop ekin))
        {
            // Ekin büyümüþ veya çürümüþ mü?
            if (ekin.IsGrown || ekin.IsRotted)
            {
                if (ekin.TryGetComponent(out NetworkObject netObj))
                {
                    // Çifte kesimi önlemek için objenin hala aðda var olduðundan emin ol
                    if (netObj.IsSpawned)
                    {
                        bool urunVerecekMi = ekin.IsGrown; // Saðlýklýysa ürün verir
                        HasatEtServerRpc(netObj.NetworkObjectId, ekin.transform.position, urunVerecekMi);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void HasatEtServerRpc(ulong ekinObjId, Vector3 ekinPozisyonu, bool urunVer)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ekinObjId, out NetworkObject obj))
        {
            // 1. Ürün Saçma
            if (urunVer)
            {
                TohumVerisi v = TerrainLayerManager.Instance.tohumListesi.Find(x => obj.name.Contains(x.tohumAdi));
                if (v != null)
                {
                    for (int i = 0; i < v.hasatMiktari; i++)
                    {
                        Vector3 off = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                        GameObject t = Instantiate(v.dusecekTohumPrefab, ekinPozisyonu + off, Quaternion.identity);
                        t.GetComponent<NetworkObject>().Spawn();
                    }
                }
            }

            // 2. Tarlayý Kurutma
            bool wasWet = TerrainLayerManager.Instance.IsSoilWet(ekinPozisyonu);

            // 3. Ekini Yok Et
            obj.Despawn();
            Destroy(obj.gameObject);

            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(ekinPozisyonu, TerrainLayerManager.Instance.tilledLayerIndex);
            }
        }
    }
}