using Unity.Netcode;
using UnityEngine;

public class CubeInteractable : NetworkBehaviour, IInteractable
{
    [Header("Ayarlar")]
    public GameObject spherePrefab;

    [Tooltip("1: Beyaz, 2: Sarı, 3: Kırmızı vs.")]
    public int verilecekTohumID;

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            inventory.currentSeedID = verilecekTohumID;
        }

        SpawnSphereServerRpc(interactor.OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SpawnSphereServerRpc(ulong clientId)
    {
        if (spherePrefab == null) return;

        GameObject spawnedSphere = Instantiate(spherePrefab, transform.position + Vector3.up, Quaternion.identity);

        if (spawnedSphere.TryGetComponent(out NetworkObject sphereObj))
        {
            sphereObj.SpawnWithOwnership(clientId);

            if (spawnedSphere.TryGetComponent(out SphereFollower follower))
            {
                follower.SetTargetClientRpc(clientId);
            }
        }
    }
}