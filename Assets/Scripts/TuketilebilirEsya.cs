using UnityEngine;
using Unity.Netcode;

public class TuketilebilirEsya : MonoBehaviour, IUseableTool
{
    [Header("Eþya Özellikleri")]
    public bool buBirSuMudur = false;
    public float verilecekEnerji = 25f;

    public void EylemYap(RaycastHit hit, PlayerInventory inventory)
    {
        if (inventory.TryGetComponent(out PlayerEnergy enerjiSistemi))
        {
            // 1. Enerjiyi ver
            enerjiSistemi.TuketimYapServerRpc(buBirSuMudur, verilecekEnerji);

            // 2. Envanterden bir adet eksiltmek için ServerRpc gönder
            // Bu iþlem için PlayerInventory içinde özel bir tüketim metodu çaðýrýyoruz
            inventory.EldeTuketimYapServerRpc();
        }
    }
}