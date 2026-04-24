using Unity.Netcode;
using UnityEngine;

public class PickupableTool : NetworkBehaviour, IInteractable
{
    [Header("Alet Ayarları")]
    public ToolType aletTipi;

    [Header("Takip Ayarları")]
    public Vector3 offset = new Vector3(0.5f, -0.4f, 1f);
    public float followSpeed = 10f;

    public NetworkVariable<bool> isEquipped = new NetworkVariable<bool>(false);

    private Transform targetCamera;
    private Collider aletCollider;
    private Rigidbody rb;

    private void Awake()
    {
        aletCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        isEquipped.OnValueChanged += OnEquipStateChanged;
        DurumuGuncelle(isEquipped.Value);
    }

    public override void OnNetworkDespawn()
    {
        isEquipped.OnValueChanged -= OnEquipStateChanged;
    }

    private void OnEquipStateChanged(bool eski, bool yeni) { DurumuGuncelle(yeni); }

    private void DurumuGuncelle(bool equipped)
    {
        if (equipped)
        {
            if (rb != null) rb.isKinematic = true;
            if (aletCollider != null) aletCollider.enabled = false;
        }
        else
        {
            if (rb != null) rb.isKinematic = false;
            if (aletCollider != null) aletCollider.enabled = true;
            targetCamera = null;
        }
    }

    public void Interact(NetworkObject interactor)
    {
        if (isEquipped.Value) return;

        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            if (inventory.aktifAlet != ToolType.Yok) inventory.EldekiniYereAt();
            AlServerRpc(interactor.OwnerClientId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void AlServerRpc(ulong oyuncuID)
    {
        NetworkObject.ChangeOwnership(oyuncuID);
        isEquipped.Value = true;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(oyuncuID, out NetworkClient client))
        {
            if (client.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
                // Tohum id ve miktarı gitti, sadece objeyi ve tipini yolluyor
                inventory.AletKusanServerRpc(NetworkObject, aletTipi);
            }
        }
    }

    public void YereFirlat(Vector3 pozisyon, Vector3 yon) { YereFirlatServerRpc(pozisyon, yon); }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void YereFirlatServerRpc(Vector3 pozisyon, Vector3 yon)
    {
        NetworkObject.RemoveOwnership();
        isEquipped.Value = false;

        transform.position = pozisyon + yon * 1f;
        if (rb != null) rb.AddForce(yon * 5f, ForceMode.Impulse);
    }

    void Update()
    {
        if (!IsSpawned || !isEquipped.Value) return;

        if (targetCamera == null)
        {
            NetworkObject playerObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(OwnerClientId);
            if (playerObj != null && playerObj.TryGetComponent(out PlayerInteractor interactor) && interactor.playerCamera != null)
            {
                targetCamera = interactor.playerCamera;
            }
            return;
        }

        Vector3 targetPos = targetCamera.position + targetCamera.TransformDirection(offset);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetCamera.rotation, Time.deltaTime * followSpeed);
    }
}