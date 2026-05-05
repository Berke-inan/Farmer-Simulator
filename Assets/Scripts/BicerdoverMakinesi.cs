using UnityEngine;
using Unity.Netcode;

public class BicerdoverMakinesi : NetworkBehaviour
{
    private AttachableEquipment anaGovde;

    [Header("Hasat Ayarlarý")]
    [Tooltip("Makinenin ekinleri keseceði alanýn geniþliði")]
    public float kesimYaricapi = 2.5f;
    [Tooltip("Saniyede kaç kere etrafý tarasýn?")]
    public float islemAraligi = 0.2f;
    private float islemSayaci = 0f;

    private void Awake()
    {
        anaGovde = GetComponentInParent<AttachableEquipment>();
        if (anaGovde == null) Debug.LogError("DÝKKAT: BicerdoverMakinesi kodu, AttachableEquipment ile ayný veya alt objede olmalý!");
    }

    private void Update()
    {
        // 1. Ýzinleri kontrol et
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        // 2. Performans sayacý
        islemSayaci += Time.deltaTime;
        if (islemSayaci < islemAraligi) return;

        // 3. Etrafý Tara (OrakEylemi'ndeki mantýk)
        // Lazer yerine geniþ bir küre ile etrafý tarýyoruz ki makinenin aðzýna giren her þeyi alsýn
        Collider[] etraftakiler = Physics.OverlapSphere(transform.position, kesimYaricapi);

        bool hasatYapildiMi = false;

        foreach (var col in etraftakiler)
        {
            if (col.TryGetComponent(out ModularCrop ekin))
            {
                // Ekin büyümüþ veya çürümüþse
                if (ekin.IsGrown || ekin.IsRotted)
                {
                    if (ekin.TryGetComponent(out NetworkObject netObj))
                    {
                        bool urunVerecekMi = ekin.IsGrown; // Saðlýklýysa ürün verir
                        HasatEtServerRpc(netObj.NetworkObjectId, ekin.transform.position, urunVerecekMi);
                        hasatYapildiMi = true;
                    }
                }
            }
        }

        // Eðer en az 1 ekin kestiysek sayacý sýfýrla
        if (hasatYapildiMi) islemSayaci = 0f;
    }

    [Rpc(SendTo.Server)]
    private void HasatEtServerRpc(ulong ekinObjId, Vector3 ekinPozisyonu, bool urunVer)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ekinObjId, out NetworkObject obj))
        {
            // 1. Ürün Saçma (Arkadaþýnýn Kodu)
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

            // 2. Tarlayý Kurutma (Arkadaþýnýn Kodu)
            bool wasWet = TerrainLayerManager.Instance.IsSoilWet(ekinPozisyonu);

            // 3. Ekini Yok Et
            obj.Despawn();
            Destroy(obj.gameObject);

            if (wasWet)
            {
                TerrainLayerManager.Instance.PaintSoilServerRpc(ekinPozisyonu, TerrainLayerManager.Instance.tilledLayerIndex,3);
            }
        }
    }

    // Hasat alanýný Unity editöründe kýrmýzý bir küre olarak görmek için
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawSphere(transform.position, kesimYaricapi);
    }
}