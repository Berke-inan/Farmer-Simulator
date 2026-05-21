using UnityEngine;
using Unity.Netcode;

public class BreakableTarget : NetworkBehaviour
{
    private Animator animator;
    private bool isBroken = false;

    private void Awake()
    {
        // Deðiþiklik: Eðer Animator alt objelerden birindeyse otomatik bulur!
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("<color=red>UYARI: " + gameObject.name + " objesinde ve alt elemanlarýnda hiçbir Animator bileþeni bulunamadý!</color>");
        }
    }

    public void TakeDamage()
    {
        if (isBroken) return;
        BreakServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void BreakServerRpc()
    {
        if (isBroken) return;
        isBroken = true;

        PlayBreakAnimationClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void PlayBreakAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Break");
            Debug.Log("<color=cyan>Animator tetiklendi! Break animasyonu oynatýlýyor...</color>");
        }

        // Animasyon bittikten sonra objeyi sahneden tamamen temizle
        Destroy(gameObject, 1f);
    }
}