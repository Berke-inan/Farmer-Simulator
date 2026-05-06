using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PickupableTool))]
public class SepetEsyasi : NetworkBehaviour
{
    [Header("Sepet Modelleri")]
    public GameObject modelBos;
    public GameObject modelYarim;
    public GameObject modelDolu;

    [Header("Toplama Ayarlarý")]
    public float toplamaMenzili = 4f;

    // 0 = Boþ, 1 = Yarým, 2 = Tam Dolu
    public NetworkVariable<int> sepetDoluluk = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private PickupableTool pickupTool;

    private void Awake()
    {
        pickupTool = GetComponent<PickupableTool>();
    }

    public override void OnNetworkSpawn()
    {
        sepetDoluluk.OnValueChanged += DolulukDegistigindeGorselleriGuncelle;
        GorselleriGuncelle(sepetDoluluk.Value); // Ýlk doðduðunda modeli ayarla
    }

    public override void OnNetworkDespawn()
    {
        sepetDoluluk.OnValueChanged -= DolulukDegistigindeGorselleriGuncelle;
    }

    private void DolulukDegistigindeGorselleriGuncelle(int eskiDurum, int yeniDurum)
    {
        GorselleriGuncelle(yeniDurum);
    }

    // Sepetin içindeki meyve modellerini doluluða göre açýp kapatýr
    private void GorselleriGuncelle(int durum)
    {
        if (modelBos != null) modelBos.SetActive(durum == 0);
        if (modelYarim != null) modelYarim.SetActive(durum == 1);
        if (modelDolu != null) modelDolu.SetActive(durum >= 2);
    }

    private void Update()
    {
        // Bizim deðilse veya elimizde takýlý deðilse iþlem yapma
        if (!IsOwner || !pickupTool.isEquipped.Value || pickupTool.isStored.Value) return;

        // F Tuþuna basýldýðýnda
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (sepetDoluluk.Value >= 2)
            {
                Debug.Log("Sepet tamamen dolu! Daha fazla meyve alamazsýn.");
                return;
            }

            if (pickupTool.targetCamera != null)
            {
                Ray ray = new Ray(pickupTool.targetCamera.position, pickupTool.targetCamera.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, toplamaMenzili))
                {
                    // Týkladýðýmýz þey bir aðaç mý?
                    TreeController agac = hit.collider.GetComponentInParent<TreeController>();
                    if (agac != null && agac.mevcutDurum.Value == TreeState.Meyveli)
                    {
                        ToplamaIstegiGonderServerRpc(agac.NetworkObjectId);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void ToplamaIstegiGonderServerRpc(ulong agacID)
    {
        if (sepetDoluluk.Value >= 2) return;

        // Sunucuda aðacý bul
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(agacID, out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out TreeController agac))
            {
                // Aðaçtan meyveyi çekiriz, eðer baþarýlýysa (true dönerse) sepeti doldururuz
                bool hasatBasarili = agac.MeyveHasatEt();
                if (hasatBasarili)
                {
                    sepetDoluluk.Value++;
                }
            }
        }
    }
}