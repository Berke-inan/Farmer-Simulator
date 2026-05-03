using Unity.Netcode;
using UnityEngine;

public class PickupableTool : NetworkBehaviour, IInteractable
{
    [Header("Alet Ayarları")]
    public ToolType aletTipi;

    [Header("Takip Ayarları")]
    public Vector3 offset = new Vector3(0.5f, -0.4f, 1f);
    public float followSpeed = 10f;

    [Header("Depolama Ayarları")]
    public bool isStoreable = true;

    public NetworkVariable<bool> isEquipped = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isStored = new NetworkVariable<bool>(false);

    public Transform targetCamera;
    private Collider aletCollider;
    private Rigidbody rb;

    private void Awake()
    {
        aletCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkSpawn()
    {
        isEquipped.OnValueChanged += OnStateChanged;
        isStored.OnValueChanged += OnStateChanged;
        DurumuGuncelle();
    }

    public override void OnNetworkDespawn()
    {
        isEquipped.OnValueChanged -= OnStateChanged;
        isStored.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(bool eski, bool yeni) { DurumuGuncelle(); }

    private void DurumuGuncelle()
    {
        // Elde veya depodaysa fizikleri ve collider'ı TAMAMEN kapat
        if (isEquipped.Value || isStored.Value)
        {
            if (rb != null) rb.isKinematic = true;
            if (aletCollider != null) aletCollider.enabled = false;
            if (isStored.Value) targetCamera = null;
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
        // Römorktaysa veya eldeyse doğrudan alınamaz
        if (isEquipped.Value || isStored.Value) return;

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
        isStored.Value = false;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(oyuncuID, out NetworkClient client))
        {
            if (client.PlayerObject.TryGetComponent(out PlayerInventory inventory))
            {
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
        isStored.Value = false;

        transform.position = pozisyon + yon * 1f;
        if (rb != null) rb.AddForce(yon * 5f, ForceMode.Impulse);
    }

    void Update()
    {
        if (!IsSpawned || !isEquipped.Value || isStored.Value) return;

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