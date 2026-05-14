using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class YakitIstasyonu : NetworkBehaviour
{
    [Header("istasyon ayarlari")]
    public NetworkVariable<float> istasyonYakiti = new NetworkVariable<float>(1000f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("bidon dolum ayarlari")]
    public float algilamaMesafesi = 6f;
    public float bidonDolumHizi = 25f; // saniyede 25l

    private float istasyonAktarimBirikimi = 0f;

    // pompanin kullanacagi yakit cekme sistemi (sunucu tarafindan cagrilir)
    public float YakitCek(float istenenMiktar)
    {
        if (istasyonYakiti.Value <= 0) return 0f;

        float verilecek = Mathf.Min(istenenMiktar, istasyonYakiti.Value);
        istasyonYakiti.Value -= verilecek;

        return verilecek;
    }

    private void Update()
    {
        // sadece yerel oyuncu icin r tusuna basma kontrolu yapýyoruz
        if (!IsSpawned || !IsClient) return;

        if (Keyboard.current != null && Keyboard.current.rKey.isPressed)
        {
            var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayer == null) return;

            PlayerInteractor pi = localPlayer.GetComponent<PlayerInteractor>();
            PlayerInventory inventory = localPlayer.GetComponent<PlayerInventory>();

            if (pi == null || pi.playerCamera == null || inventory == null) return;

            // mesafe kontrolü
            if (Vector3.Distance(transform.position, localPlayer.transform.position) > algilamaMesafesi) return;

            Ray ray = new Ray(pi.playerCamera.position, pi.playerCamera.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, algilamaMesafesi))
            {
                // istasyona mý bakýyoruz
                if (hit.collider.GetComponentInParent<YakitIstasyonu>() == this)
                {
                    // elinde yakit bidonu scripti takýlý bir görsel var mý bakýyoruz
                    if (inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out YakitBidonu bidonVisual))
                    {
                        // yakit verisi artik inventory icinde senkronize duruyor
                        if (inventory.bidonMevcutYakit.Value < 25f && istasyonYakiti.Value > 0) // 25f yerine bidon max kapasitesi yazýlabilir
                        {
                            istasyonAktarimBirikimi += bidonDolumHizi * Time.deltaTime;
                            if (istasyonAktarimBirikimi >= 2.5f)
                            {
                                IstasyondanBidonaServerRpc(localPlayer.NetworkObjectId, istasyonAktarimBirikimi);
                                istasyonAktarimBirikimi = 0f;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            istasyonAktarimBirikimi = 0f;
        }
    }

    [Rpc(SendTo.Server)]
    private void IstasyondanBidonaServerRpc(ulong playerNetId, float miktar)
    {
        if (istasyonYakiti.Value < miktar) miktar = istasyonYakiti.Value;
        if (miktar <= 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetId, out NetworkObject playerObj))
        {
            if (playerObj.TryGetComponent(out PlayerInventory inventory))
            {
                // artik bidonun kendisi yerine oyuncunun envanterindeki yakit degiskenini guncelliyoruz
                float maxBidonKapasitesi = 25f; // bu degeri istersen itemdata icinden de cekebiliriz
                float bosYer = maxBidonKapasitesi - inventory.bidonMevcutYakit.Value;
                float eklenecek = Mathf.Min(miktar, bosYer);

                if (eklenecek > 0)
                {
                    inventory.bidonMevcutYakit.Value += eklenecek;
                    istasyonYakiti.Value -= eklenecek;
                }
            }
        }
    }
}