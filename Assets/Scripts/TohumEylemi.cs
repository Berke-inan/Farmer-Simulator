using Unity.Netcode;
using UnityEngine;

public class TohumEylemi : NetworkBehaviour, IUseableTool
{
    [Header("Tohum Özellikleri")]
    public int tohumID = 1;

    // Ağ üzerinden senkronize edilen kullanım hakkı (biri yere atıp başkası alırsa hak aynen kalır)
    public NetworkVariable<int> kalanMiktar = new NetworkVariable<int>(4);

    public void EylemYap(SoilTile hedefToprak, PlayerInventory envanter)
    {
        // Eğer toprak çapalanmışsa ve tohum hakkımız varsa ekim yap
        if (hedefToprak.MevcutDurum == SoilState.Tilled && kalanMiktar.Value > 0)
        {
            hedefToprak.TohumEkServerRpc(tohumID);
            MiktariAzaltServerRpc(envanter.NetworkObjectId);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void MiktariAzaltServerRpc(ulong envanterID)
    {
        kalanMiktar.Value--;

        // Tohum hakkı bittiyse kendini yok etmesi için oyuncunun envanterine emir yolla
        if (kalanMiktar.Value <= 0)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(envanterID, out NetworkObject playerObj))
            {
                if (playerObj.TryGetComponent(out PlayerInventory envanter))
                {
                    envanter.EldekiniYokEtServerRpc();
                }
            }
        }
    }
}