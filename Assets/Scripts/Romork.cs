using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class Romork : NetworkBehaviour, IInteractable
{
    [Header("Dizilim Ayarları")]
    public Transform kargoNoktasi;

    [Header("Grid Kapasitesi")]
    public int sutunSayisi = 2;
    public int satirSayisi = 3;
    public int maksimumKat = 2;

    [Header("Mesafe Ayarları")]
    public float aralikX = 2.2f;
    public float aralikZ = 0.6f;
    public float yiginYuksekligi = 0.6f;

    private Stack<NetworkObject> icindekiEsyalar = new Stack<NetworkObject>();

    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            if (inventory.aktifAlet != ToolType.Yok && inventory.eldekiObje != null)
            {
                if (inventory.eldekiObje.TryGetComponent(out PickupableTool tool))
                {
                    if (tool.isStoreable)
                    {
                        int maksimumKapasite = sutunSayisi * satirSayisi * maksimumKat;
                        if (icindekiEsyalar.Count >= maksimumKapasite)
                        {
                            Debug.Log("Römork tamamen dolu!");
                            return;
                        }

                        RomorkaKoyServerRpc(inventory.eldekiObje);
                        inventory.EnvanteriTemizleServerRpc();
                    }
                }
            }
            else if (inventory.aktifAlet == ToolType.Yok)
            {
                RomorktanAlServerRpc(interactor.OwnerClientId);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RomorkaKoyServerRpc(NetworkObjectReference objeRef)
    {
        if (objeRef.TryGet(out NetworkObject obje))
        {
            obje.RemoveOwnership();

            if (obje.TryGetComponent(out PickupableTool tool))
            {
                // Anında fizik ve collider kapatma (Gecikme önlemi)
                if (tool.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
                if (tool.TryGetComponent(out Collider col)) col.enabled = false;

                tool.isStored.Value = true;
                tool.isEquipped.Value = false;
            }

            icindekiEsyalar.Push(obje);
            obje.TrySetParent(transform);

            int sira = icindekiEsyalar.Count - 1;
            int katKapasitesi = sutunSayisi * satirSayisi;

            int katIndex = sira / katKapasitesi;
            int katIciSira = sira % katKapasitesi;

            int xIndex = katIciSira % sutunSayisi;
            int zIndex = katIciSira / sutunSayisi;

            Vector3 yerelOffset = new Vector3(xIndex * aralikX, katIndex * yiginYuksekligi, -(zIndex * aralikZ));
            Vector3 nihaiLokalPozisyon = kargoNoktasi.localPosition + yerelOffset;
            Quaternion nihaiLokalRotasyon = kargoNoktasi.localRotation;

            SetClientLocalTransformRpc(objeRef, nihaiLokalPozisyon, nihaiLokalRotasyon);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SetClientLocalTransformRpc(NetworkObjectReference objeRef, Vector3 localPos, Quaternion localRot)
    {
        if (objeRef.TryGet(out NetworkObject obje))
        {
            // İstemcilerde de anında fizik ve collider kapatma
            if (obje.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            if (obje.TryGetComponent(out Collider col)) col.enabled = false;

            obje.transform.localPosition = localPos;
            obje.transform.localRotation = localRot;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RomorktanAlServerRpc(ulong oyuncuID)
    {
        if (icindekiEsyalar.Count == 0) return;

        NetworkObject alinanObje = icindekiEsyalar.Pop();

        if (alinanObje != null && alinanObje.IsSpawned)
        {
            alinanObje.TryRemoveParent();
            alinanObje.ChangeOwnership(oyuncuID);

            if (alinanObje.TryGetComponent(out PickupableTool tool))
            {
                tool.isEquipped.Value = true;
                tool.isStored.Value = false;

                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(oyuncuID, out NetworkClient client))
                {
                    if (client.PlayerObject.TryGetComponent(out PlayerInventory inventory))
                    {
                        inventory.AletKusanServerRpc(alinanObje, tool.aletTipi);
                    }
                }
            }
        }
    }
}