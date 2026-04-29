using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// IInteractable silindi, yerine ISecondaryInteractable eklendi
public class BalyalanabilirObje : NetworkBehaviour, ISecondaryInteractable
{
    [Header("Balya Ayarları")]
    public string objeTipi = "Misir";
    public GameObject balyaPrefab;
    public float aramaYaricapi = 2f;

    // Fonksiyonun adı arayüze uygun olarak SecondaryInteract yapıldı
    public void SecondaryInteract(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            // Sadece elinde hiçbir şey yoksa (boş elleyse) balyalama yapabilir
            if (inventory.aktifAlet == ToolType.Yok)
            {
                BalyalaServerRpc();
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void BalyalaServerRpc()
    {
        Collider[] etraftakiler = Physics.OverlapSphere(transform.position, aramaYaricapi);
        List<BalyalanabilirObje> ayniTipler = new List<BalyalanabilirObje>();

        foreach (var col in etraftakiler)
        {
            if (col.TryGetComponent(out BalyalanabilirObje obje))
            {
                if (obje.objeTipi == this.objeTipi)
                {
                    ayniTipler.Add(obje);
                }
            }
        }

        if (ayniTipler.Count >= 3)
        {
            for (int i = 0; i < 3; i++)
            {
                ayniTipler[i].GetComponent<NetworkObject>().Despawn();
            }

            if (balyaPrefab != null)
            {
                GameObject balya = Instantiate(balyaPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                balya.GetComponent<NetworkObject>().Spawn();
            }
        }
    }
}