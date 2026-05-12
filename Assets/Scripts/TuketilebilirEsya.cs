using UnityEngine;
using Unity.Netcode; // Sadece NetworkObject kontrolü için durabilir

// Sadece MonoBehaviour ve IUseableTool! Eski aðýrlýklardan kurtulduk.
public class TuketilebilirEsya : MonoBehaviour, IUseableTool
{
    [Header("Eþya Özellikleri")]
    [Tooltip("Eðer bu tikliyse Max Enerjiyi artýrýr (Su mantýðý). Tikli deðilse normal enerjiyi artýrýr (Yemek mantýðý)")]
    public bool buBirSuMudur = false;

    [Tooltip("Tüketildiðinde enerjiyi/max enerjiyi kaç puan artýracak?")]
    public float verilecekEnerji = 25f;

    // Oyuncu eline alýp sol týka (veya etkileþim tuþuna) bastýðýnda direkt bu çalýþýr
    public void EylemYap(RaycastHit hit, InventoryManager inv)
    {
        // Envanter yöneticisinin olduðu obje ayný zamanda Player'ýn kendisidir.
        if (inv.TryGetComponent(out PlayerEnergy enerjiSistemi) && inv.TryGetComponent(out NetworkedHotbar hotbar))
        {
            // 1. Enerjiyi ver (Senin yazdýðýn orijinal PlayerEnergy scriptindeki RPC'yi tetikliyoruz)
            enerjiSistemi.TuketimYapServerRpc(buBirSuMudur, verilecekEnerji);

            // 2. Tüketildiði için envanterden 1 tane eksilt. 
            // (Eðer sonuncuysa eldeki görsel de otomatik yok olur)
            inv.RemoveItemServerRpc(hotbar.ActiveSlotIndex.Value, 1, transform.position, Vector3.zero, false);
        }
    }
}