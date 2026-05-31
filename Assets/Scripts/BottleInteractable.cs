using UnityEngine;
using Unity.Netcode;
using System.Collections; // Coroutine (Gecikme) kullanmak için bu þart

public class BottleInteractable : NetworkBehaviour, IInteractable
{
    public int bosKovaID = 11;
    public int doluKovaID = 12;
    public GameObject doluSiseFizikselPrefab;

    private bool isFilled = false; // Oyuncunun ayný þiþeye 2 kere týklamasýný engeller

    public void Interact(NetworkObject interactor)
    {
        PlayerInventory inventory = interactor.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        int activeIndex = inventory.activeHotbarIndex.Value;
        InventorySlot activeSlot = inventory.slots[activeIndex];

        if (!activeSlot.IsEmpty && activeSlot.itemData != null && activeSlot.itemData.itemID == doluKovaID)
        {
            SiseyeDoldurServerRpc(interactor.NetworkObjectId, activeIndex);
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SiseyeDoldurServerRpc(ulong playerId, int slotIndex)
    {
        if (isFilled) return; // Zaten doluyorsa iþlemi durdur
        isFilled = true;

        // 1. Önce oyuncuya "Kovaný boþalt ve eline Boþ Kova al" mesajýný gönderiyoruz
        SiseyeDoldurClientRpc(playerId, slotIndex);

        // 2. Masaya fiziksel olarak Dolu Þiþe spawnlýyoruz
        if (doluSiseFizikselPrefab != null)
        {
            GameObject doluSise = Instantiate(doluSiseFizikselPrefab, transform.position, transform.rotation);
            NetworkObject netObj = doluSise.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }

        // 3. Kendimizi HEMEN SÝLMÝYORUZ! Mesajýn oyuncuya ulaþmasý için 0.2 sn zaman tanýyoruz
        StartCoroutine(YokOlmaGecikmesi());
    }

    private IEnumerator YokOlmaGecikmesi()
    {
        // Oyuncu silindiðini sansýn ve anýnda tepki alsýn diye objeyi görünmez yapýyoruz
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;

        // Mesajýn að üzerinden oyuncuya ulaþmasý için saliselik bekleme
        yield return new WaitForSeconds(0.2f);

        // Sinyal kesinlikle ulaþtý, artýk sunucudan tamamen silebiliriz!
        if (IsServer) GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Everyone)]
    private void SiseyeDoldurClientRpc(ulong playerId, int slotIndex)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerId, out NetworkObject playerObj))
        {
            if (playerObj.IsOwner)
            {
                PlayerInventory inventory = playerObj.GetComponent<PlayerInventory>();

                // 1 Adet dolu kovayý sil
                inventory.DecreaseItemAmountServerRpc(slotIndex, 1);

                // 1 Adet boþ kovayý çantaya/ele ver
                inventory.GiveSpecificItemServerRpc(bosKovaID, 1);
            }
        }
    }
}