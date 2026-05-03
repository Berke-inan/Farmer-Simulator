using Unity.Netcode;
using UnityEngine;

public enum ToolType { Yok, Tohum, Capa, SulamaKabi, Orak, Balya , YakitBidonu } 

public class PlayerInventory : NetworkBehaviour
{
    [Header("Aktif Durum")]
    public ToolType aktifAlet = ToolType.Yok;
    public NetworkObject eldekiObje;

    public void EldekiniYereAt()
    {
        if (aktifAlet == ToolType.Yok || eldekiObje == null) return;

        if (eldekiObje.TryGetComponent(out PickupableTool tool))
        {
            Vector3 yon = transform.forward;
            Vector3 pozisyon = transform.position + Vector3.up;

            if (TryGetComponent(out PlayerInteractor interactor) && interactor.playerCamera != null)
            {
                yon = interactor.playerCamera.forward;
                pozisyon = interactor.playerCamera.position;
            }

            tool.YereFirlat(pozisyon, yon);
        }
        else
        {
            EldekiniYokEtServerRpc();
        }

        EnvanteriTemizleServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AletKusanServerRpc(NetworkObjectReference objeRef, ToolType tip)
    {
        AgaBildirRpc(objeRef, tip);
    }

    [Rpc(SendTo.Everyone)]
    private void AgaBildirRpc(NetworkObjectReference objeRef, ToolType tip)
    {
        if (objeRef.TryGet(out NetworkObject agObjesi))
        {
            eldekiObje = agObjesi;
        }
        aktifAlet = tip;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EldekiniYokEtServerRpc()
    {
        if (eldekiObje != null && eldekiObje.IsSpawned)
        {
            eldekiObje.Despawn();
            eldekiObje = null;
        }
        EnvanteriTemizleServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void EnvanteriTemizleServerRpc()
    {
        AgaTemizleRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void AgaTemizleRpc()
    {
        aktifAlet = ToolType.Yok;
        eldekiObje = null;
    }
}