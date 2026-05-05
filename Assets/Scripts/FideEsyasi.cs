using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PickupableTool))]
public class FideEsyasi : NetworkBehaviour // DÝKKAT: IUseableTool arayüzünü sildik! Artýk sadece F'ye tepki verecek.
{
    [Header("Ekim Ayarlarý")]
    [Tooltip("Dikilecek asýl aðaç prefabý (Üzerinde TreeController olan)")]
    public GameObject agacPrefab;

    [Tooltip("Ýki aðaç veya ekin arasý býrakýlmasý gereken minimum mesafe")]
    public float minimumEkimMesafesi = 1.0f;
    
    [Tooltip("Kameradan ne kadar uzaða ekim yapýlabilir?")]
    public float ekimMenzili = 4f;

    private PickupableTool pickupTool;

    private void Awake()
    {
        pickupTool = GetComponent<PickupableTool>();
    }

    private void Update()
    {
        // Eðer obje bizim deðilse veya elimizde (aktif) deðilse hiçbir þey yapma
        if (!IsOwner || !pickupTool.isEquipped.Value || pickupTool.isStored.Value) return;

        // Klavyeden F tuþuna bir kere BASILDIÐINDA çalýþýr (basýlý tutma deðil, tek týk)
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (pickupTool.targetCamera != null)
            {
                // Kameranýn tam ortasýndan ileriye doðru görünmez bir lazer at
                Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, ekimMenzili))
                {
                    // 1. Týkladýðýmýz obje bir Terrain mi kontrol et
                    if (hit.collider is TerrainCollider tCol)
                    {
                        // 2. Terrain üzerinde senin katman yöneticin var mý?
                        TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();
                        
                        if (manager != null)
                        {
                            // 3. Týkladýðýmýz spesifik nokta çapalanmýþ (boyanmýþ) mý?
                            bool isTilled = manager.IsSoilTilled(hit.point);
                            
                            if (isTilled)
                            {
                                // 4. Güvenlik: Ayný noktaya üst üste bitki veya aðaç ekilmesini engelle
                                Collider[] yakindakiler = Physics.OverlapSphere(hit.point, minimumEkimMesafesi);
                                bool yakinlardaBaskaBitkiVar = false;

                                foreach (var col in yakindakiler)
                                {
                                    if (col.GetComponent<ModularCrop>() != null || col.GetComponent<TreeController>() != null) 
                                    { 
                                        yakinlardaBaskaBitkiVar = true; 
                                        break; 
                                    }
                                }

                                if (!yakinlardaBaskaBitkiVar)
                                {
                                    // Her þey mükemmel! Local oyuncunun envanterini bulup fideyi elden at
                                    var localClient = NetworkManager.Singleton.LocalClient;
                                    if (localClient != null && localClient.PlayerObject != null)
                                    {
                                        PlayerInventory inventory = localClient.PlayerObject.GetComponent<PlayerInventory>();
                                        if (inventory != null)
                                        {
                                            inventory.EldekiniYereAt();
                                            DikVeYokOlServerRpc(hit.point);
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.Log("Buraya çok yakýn baþka bir bitki veya aðaç var!");
                                }
                            }
                            else
                            {
                                Debug.Log("Fideyi ekmek için önce bu topraðý çapalayýp/kazmalýsýn!");
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Fide sadece tarlaya (Terrain üzerine) ekilebilir!");
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void DikVeYokOlServerRpc(Vector3 dikimNoktasi)
    {
        // Gerçek aðacý sunucuda yarat ve aða dahil et
        GameObject yeniAgac = Instantiate(agacPrefab, dikimNoktasi, Quaternion.identity);
        yeniAgac.GetComponent<NetworkObject>().Spawn();

        // Elimizi terk eden bu minik fide eþyasýný oyundan kalýcý olarak sil
        GetComponent<NetworkObject>().Despawn();
    }
}