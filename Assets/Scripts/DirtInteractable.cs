using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class TohumAsamaVerisi
{
    public string tohumAdi;

    [Tooltip("Bu tohumun bir sonraki aşamaya geçmesi için gereken süre (Saniye)")]
    public float asamaGecisSuresi = 10f; // Süre değişkeni buraya taşındı

    public GameObject[] asamalar;
}

public class DirtInteractable : NetworkBehaviour, IInteractable
{
    [Header("Ekin Ayarları")]
    public TohumAsamaVerisi[] tohumListesi;

    private NetworkVariable<int> ekiliTohumID = new NetworkVariable<int>(0);
    private NetworkVariable<int> mevcutAsama = new NetworkVariable<int>(0);

    private float buyumeSayaci = 0f;

    public override void OnNetworkSpawn()
    {
        ekiliTohumID.OnValueChanged += OnVeriDegisti;
        mevcutAsama.OnValueChanged += OnVeriDegisti;

        GorselleriGuncelle();
    }

    public override void OnNetworkDespawn()
    {
        ekiliTohumID.OnValueChanged -= OnVeriDegisti;
        mevcutAsama.OnValueChanged -= OnVeriDegisti;
    }

    public void Interact(NetworkObject interactor)
    {
        if (ekiliTohumID.Value != 0) return;

        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            if (inventory.currentSeedID != 0)
            {
                TohumEkServerRpc(inventory.currentSeedID);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void TohumEkServerRpc(int ekilecekID)
    {
        ekiliTohumID.Value = ekilecekID;
        mevcutAsama.Value = 0;
        buyumeSayaci = 0f;
    }

    void Update()
    {
        if (!IsServer) return;

        if (ekiliTohumID.Value > 0)
        {
            int tohumIndex = ekiliTohumID.Value - 1;

            if (tohumIndex >= 0 && tohumIndex < tohumListesi.Length)
            {
                int maksimumAsama = tohumListesi[tohumIndex].asamalar.Length - 1;

                if (mevcutAsama.Value < maksimumAsama)
                {
                    buyumeSayaci += Time.deltaTime;

                    // Sayacı kontrol ederken, artık doğrudan o tohumun kendi özel süresine bakılıyor
                    float hedefSure = tohumListesi[tohumIndex].asamaGecisSuresi;

                    if (buyumeSayaci >= hedefSure)
                    {
                        buyumeSayaci = 0f;
                        mevcutAsama.Value++;
                    }
                }
            }
        }
    }

    private void OnVeriDegisti(int eskiDeger, int yeniDeger)
    {
        GorselleriGuncelle();
    }

    private void GorselleriGuncelle()
    {
        for (int i = 0; i < tohumListesi.Length; i++)
        {
            for (int j = 0; j < tohumListesi[i].asamalar.Length; j++)
            {
                if (tohumListesi[i].asamalar[j] != null)
                {
                    tohumListesi[i].asamalar[j].SetActive(false);
                }
            }
        }

        if (ekiliTohumID.Value > 0)
        {
            int tohumIndex = ekiliTohumID.Value - 1;

            if (tohumIndex >= 0 && tohumIndex < tohumListesi.Length)
            {
                int asamaIndex = Mathf.Clamp(mevcutAsama.Value, 0, tohumListesi[tohumIndex].asamalar.Length - 1);

                GameObject aktifGorsel = tohumListesi[tohumIndex].asamalar[asamaIndex];
                if (aktifGorsel != null)
                {
                    aktifGorsel.SetActive(true);
                }
            }
        }
    }
}