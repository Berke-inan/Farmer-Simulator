using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PickupableTool))]
public class TuketilebilirEsya : NetworkBehaviour
{
    [Header("Eþya Özellikleri")]
    [Tooltip("Eðer bu tikliyse Max Enerjiyi artýrýr (Su mantýðý). Tikli deðilse normal enerjiyi artýrýr (Yemek mantýðý)")]
    public bool buBirSuMudur = false;

    [Tooltip("Tüketildiðinde enerjiyi/max enerjiyi kaç puan artýracak?")]
    public float verilecekEnerji = 25f;

    private PickupableTool pickupTool;

    private void Awake()
    {
        pickupTool = GetComponent<PickupableTool>();
    }

    private void Update()
    {
        // Eþya bizdeyse ve elimizde tutuyorsak çalýþýr
        if (!IsOwner || !pickupTool.isEquipped.Value || pickupTool.isStored.Value) return;

        // F Tuþuna basýldýðýnda
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            // Kendi karakterimizdeki PlayerEnergy scriptini bul
            var localClient = NetworkManager.Singleton.LocalClient;
            if (localClient != null && localClient.PlayerObject != null)
            {
                PlayerEnergy enerjiSistemi = localClient.PlayerObject.GetComponent<PlayerEnergy>();
                PlayerInventory envanter = localClient.PlayerObject.GetComponent<PlayerInventory>();

                if (enerjiSistemi != null && envanter != null)
                {
                    // Tüketildiðini sunucuya bildir, eþyayý elden býrak ve yok et
                    envanter.EldekiniYereAt();
                    TuketVeYokOlServerRpc(enerjiSistemi.NetworkObjectId);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void TuketVeYokOlServerRpc(ulong oyuncuID)
    {
        // Að üzerinden oyuncuyu bul
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuID, out NetworkObject oyuncuNetObj))
        {
            if (oyuncuNetObj.TryGetComponent(out PlayerEnergy oyuncuEnerji))
            {
                // Yemek/Su verisini karakterin midesine gönder
                oyuncuEnerji.TuketimYapServerRpc(buBirSuMudur, verilecekEnerji);
            }
        }

        // Tüketilen bu objeyi oyundan sil
        GetComponent<NetworkObject>().Despawn();
    }
}