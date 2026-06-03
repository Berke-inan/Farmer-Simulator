using UnityEngine;
using Unity.Netcode;

public class BicerdoverMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;
    private BreakDisableBehavior bozulmaKontrolu;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
        bozulmaKontrolu = GetComponent<BreakDisableBehavior>();
        if (anaGovde == null) Debug.LogError("DÝKKAT: BicerdoverMakinesi kodu, AttachableEquipment ile ayný veya alt objede olmalý!");
    }

    private void OnTriggerStay(Collider other)
    {
        if (bozulmaKontrolu != null && bozulmaKontrolu.isBroken.Value) return;
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        if (other.TryGetComponent(out ModularCrop ekin))
        {
            if (ekin.IsGrown || ekin.IsRotted)
            {
                if (ekin.TryGetComponent(out NetworkObject netObj))
                {
                    if (netObj.IsSpawned)
                    {
                        bool urunVerecekMi = ekin.IsGrown;
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

            bool wasWet = TerrainLayerManager.Instance.IsSoilWet(ekinPozisyonu);

            obj.Despawn();
            Destroy(obj.gameObject);

            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(ekinPozisyonu, TerrainLayerManager.Instance.tilledLayerIndex, 3);
            }
        }
    }
}