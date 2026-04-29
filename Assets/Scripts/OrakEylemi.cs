using Unity.Netcode;
using UnityEngine;

public class OrakEylemi : NetworkBehaviour, IUseableTool
{
    public float yaricap = 2.5f;

    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        Collider[] cols = Physics.OverlapSphere(hit.point, yaricap);
        foreach (var c in cols)
        {
            if (c.TryGetComponent(out ModularCrop ekin))
            {
                // Ekin büyümüş VEYA çürümüşse orakla biçilebilir
                if (ekin.IsGrown || ekin.IsRotted)
                {
                    if (ekin.TryGetComponent(out NetworkObject n))
                    {
                        // Sadece sağlıklı şekilde büyümüşse ürün verecek
                        bool urunVerecekMi = ekin.IsGrown;
                        HasatEtServerRpc(n.NetworkObjectId, ekin.transform.position, urunVerecekMi);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void HasatEtServerRpc(ulong id, Vector3 pos, bool urunVer)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out NetworkObject obj))
        {
            // Eğer ekin sağlıklıysa etrafa ürün saç
            if (urunVer)
            {
                TohumVerisi v = TerrainLayerManager.Instance.tohumListesi.Find(x => obj.name.Contains(x.tohumAdi));
                if (v != null)
                {
                    for (int i = 0; i < v.hasatMiktari; i++)
                    {
                        Vector3 off = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                        GameObject t = Instantiate(v.dusecekTohumPrefab, pos + off, Quaternion.identity);
                        t.GetComponent<NetworkObject>().Spawn();
                    }
                }
            }
            // Ürün vermeyecekse (çürümüşse) buralar tamamen atlanır

            bool wasWet = TerrainLayerManager.Instance.IsSoilWet(pos);

            // Bitkiyi tarladan sil
            obj.Despawn();
            Destroy(obj.gameObject);

            // Eğer altındaki toprak ıslaksa, eski çapalanmış (kuru) haline çevir
            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(pos, TerrainLayerManager.Instance.tilledLayerIndex);
            }
        }
    }
}