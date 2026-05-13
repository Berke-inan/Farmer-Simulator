using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class FideEsyasi : MonoBehaviour
{
    [Header("Ekim Ayarlarý")]
    public GameObject agacPrefab;
    public float minimumEkimMesafesi = 1.0f;
    public float ekimMenzili = 4f;

    private PlayerInventory inventory;

    private void Start()
    {
        inventory = GetComponentInParent<PlayerInventory>();
    }

    private void Update()
    {
        if (inventory == null || !inventory.IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, ekimMenzili))
            {
                if (hit.collider is TerrainCollider tCol)
                {
                    TerrainLayerManager manager = tCol.GetComponent<TerrainLayerManager>();
                    if (manager != null && manager.IsSoilTilled(hit.point))
                    {
                        if (!YakinlardaBitkiVarMi(hit.point))
                        {
                            // Envanterden düþür ve sunucuda aðacý dik
                            inventory.EldekiniYereAt();
                            inventory.DikmeIstegiServerRpc(hit.point, inventory.activeHotbarIndex.Value);
                        }
                    }
                }
            }
        }
    }

    private bool YakinlardaBitkiVarMi(Vector3 nokta)
    {
        Collider[] yakindakiler = Physics.OverlapSphere(nokta, minimumEkimMesafesi);
        foreach (var col in yakindakiler)
        {
            if (col.GetComponent<ModularCrop>() != null || col.GetComponent<TreeController>() != null)
                return true;
        }
        return false;
    }
}