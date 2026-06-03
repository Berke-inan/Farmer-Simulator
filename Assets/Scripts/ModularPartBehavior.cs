using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(DurabilityManager))]
public class ModularPartBehavior : NetworkBehaviour
{
    public AudioClip breakSound;

    [Tooltip("Sadece lastiðin 3D çizimini (MeshRenderer) barýndýran alt obje")]
    public GameObject visualObject;

    private DurabilityManager durabilityManager;

    public override void OnNetworkSpawn()
    {
        durabilityManager = GetComponent<DurabilityManager>();
        durabilityManager.OnHealthEmpty += HandleBreak;
        durabilityManager.OnRepaired += HandleRepair;

        // Oyuna sonradan giren (Late-Join) oyuncular için lastiðin anlýk durumunu ayarla
        UpdateVisibility(durabilityManager.currentHealth.Value > 0);
    }

    public override void OnNetworkDespawn()
    {
        if (durabilityManager != null)
        {
            durabilityManager.OnHealthEmpty -= HandleBreak;
            durabilityManager.OnRepaired -= HandleRepair;
        }
    }

    private void HandleBreak()
    {
        if (breakSound != null) AudioSource.PlayClipAtPoint(breakSound, transform.position);

        // Patladýðýnda görseli kapat (Obje ve týklama alaný sahnede kalýr)
        UpdateVisibility(false);
    }

    private void HandleRepair()
    {
        // Yeni lastik takýldýðýnda görseli geri aç
        UpdateVisibility(true);
    }

    private void UpdateVisibility(bool isVisible)
    {
        if (visualObject != null)
        {
            visualObject.SetActive(isVisible);
        }
    }
}