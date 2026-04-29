using Unity.Netcode;
using UnityEngine;

public class TohumEylemi : NetworkBehaviour, IUseableTool
{
    public int tohumID = 1;
    public GameObject ekinPrefab;
    public NetworkVariable<int> kalanMiktar = new NetworkVariable<int>(4);

    [Header("Ekim Ayarları")]
    [Tooltip("Başka bir tohuma veya bitkiye ne kadar yaklaşabilir?")]
    public float minimumEkimMesafesi = 0.8f;

    public void EylemYap(RaycastHit hit, PlayerInventory inv)
    {
        if (hit.collider is TerrainCollider tCol)
        {
            var manager = tCol.GetComponent<TerrainLayerManager>();

            // 1. Zemin çapalanmış mı kontrolü
            if (!manager.IsSoilTilled(hit.point))
            {
                Debug.Log("Burası çapalanmamış, ekim yapılamaz.");
                return;
            }

            // 2. Etrafta başka bir ekin var mı kontrolü
            Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
            bool yakinlardaEkinVar = false;

            foreach (var col in yakindakiler)
            {
                // Kendi ektiğimiz ModularCrop scriptine sahip bir obje bulursak
                if (col.TryGetComponent(out ModularCrop ekin))
                {
                    yakinlardaEkinVar = true;
                    break;
                }
            }

            if (yakinlardaEkinVar)
            {
                Debug.Log("Buraya ekemezsin, başka bir ekine çok yakın!");
                return; // Ekme işlemini iptal et
            }

            // Her şey uygunsa ve elde tohum varsa ekimi yap
            if (kalanMiktar.Value > 0)
            {
                TohumEkServerRpc(hit.point, inv.NetworkObjectId);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void TohumEkServerRpc(Vector3 nokta, ulong invID)
    {
        kalanMiktar.Value--;

        // Ekinin toprağa gömülmemesi için hafif yukarıdan (0.05f) spawn ediyoruz
        GameObject ekin = Instantiate(ekinPrefab, nokta + Vector3.up * 0.05f, Quaternion.identity);
        ekin.GetComponent<NetworkObject>().Spawn();

        // Akıllı bitkiye ID'sini veriyoruz
        if (ekin.TryGetComponent(out ModularCrop sc))
            sc.tohumID.Value = tohumID;

        // Tohum bittiyse keseyi yok et
        if (kalanMiktar.Value <= 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(invID, out NetworkObject pObj))
            {
                if (pObj.TryGetComponent(out PlayerInventory inv))
                    inv.EldekiniYokEtServerRpc();
            }
        }
    }
}