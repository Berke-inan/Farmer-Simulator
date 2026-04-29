using UnityEngine;
using Unity.Netcode;

// Mibzerin (Ekim Makinesinin) kendi özel beyni
public class SeedingMachine : NetworkBehaviour, IInteractable
{
    // Kendi isWorking deðiþkenini sildik, yerine ana gövdeyi tanýmladýk
    private AttachableEquipment anaGovde;

    [Header("Kapasite Ayarlarý")]
    public int maxMakineKapasitesi = 50;
    public NetworkVariable<int> mevcutTohumMiktari = new NetworkVariable<int>(0);

    [Tooltip("Bu makine hangi tohumu ekiyor? (Mýsýr=1, Buðday=2 vb.)")]
    public int ekilecekTohumID = 1;

    private void Awake()
    {
        // Makinenin ana gövdesini (AttachableEquipment) buluyoruz
        anaGovde = GetComponentInParent<AttachableEquipment>();
        if (anaGovde == null)
        {
            Debug.LogError("DÝKKAT: SeedingMachine üzerinde AttachableEquipment kodu bulunamadý!");
        }
    }

    // --- 1. TARLADA ÝLERLERKEN EKÝM YAPMA SÝSTEMÝ ---
    private void OnTriggerStay(Collider other)
    {
        // Ýzni kendi isWorking'imizden deðil, anaGovde'den alýyoruz!
        if (!IsServer || anaGovde == null || !anaGovde.isWorking.Value || mevcutTohumMiktari.Value <= 0) return;

        if (other.TryGetComponent(out SoilTile toprak))
        {
            // Eðer altýmýzdaki toprak çapalanmýþ (Tilled) ise
            if (toprak.MevcutDurum == SoilState.Tilled)
            {
                // Topraða tohumu ek ve makinedeki tohumu 1 azalt
                toprak.TohumEkServerRpc(ekilecekTohumID);
                mevcutTohumMiktari.Value--;
            }
        }
    }

    // --- 2. OYUNCU MAKÝNEYE TOHUM DOLDURMAK ÝSTEDÝÐÝNDE ---
    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            // Oyuncunun elinde bir obje var mý ve o objede "TohumEylemi" scripti var mý?
            if (inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out TohumEylemi eldekiTohum))
            {
                // Makinede yer varsa iþlemi Sunucuya (Server) devret
                if (mevcutTohumMiktari.Value < maxMakineKapasitesi)
                {
                    MakineyeTohumYukleServerRpc(eldekiTohum.NetworkObjectId, interactor.NetworkObjectId);
                }
            }
        }
    }

    // --- 3. SUNUCUDA YAPILAN GÜVENLÝ AKTARIM MATEMATÝÐÝ ---
    [Rpc(SendTo.Server)]
    private void MakineyeTohumYukleServerRpc(ulong tohumObjId, ulong oyuncuId)
    {
        // Að üzerinden elimizdeki çuvalý bul
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(tohumObjId, out NetworkObject tohumNetObj))
        {
            if (tohumNetObj.TryGetComponent(out TohumEylemi tohumBag))
            {
                // 1. Lojistik Hesap: Ne kadar boþ yer var?
                int bosYer = maxMakineKapasitesi - mevcutTohumMiktari.Value;

                // 2. Alýnacak Miktar: Elimdeki ile boþ yer arasýndan en küçük olaný seç
                int eklenecekMiktar = Mathf.Min(bosYer, tohumBag.kalanMiktar.Value);

                if (eklenecekMiktar > 0)
                {
                    // Makineyi doldur
                    mevcutTohumMiktari.Value += eklenecekMiktar;

                    // Oyuncunun elindeki tohum çuvalýndan eksilt
                    tohumBag.kalanMiktar.Value -= eklenecekMiktar;

                    // Eðer çuvalýn içi tamamen boþaldýysa...
                    if (tohumBag.kalanMiktar.Value <= 0)
                    {
                        // Oyuncuyu bul ve elindekini yok etmesini söyle!
                        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(oyuncuId, out NetworkObject oyuncuNetObj))
                        {
                            if (oyuncuNetObj.TryGetComponent(out PlayerInventory envanter))
                            {
                                envanter.EldekiniYokEtServerRpc();
                            }
                        }
                    }
                }
            }
        }
    }
}