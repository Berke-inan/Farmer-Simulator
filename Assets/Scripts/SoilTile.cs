using Unity.Netcode;
using UnityEngine;

public enum SoilState { Normal, Tilled, Planted, Watered, Grown }

[System.Serializable]
public class TohumAsamaVerisi
{
    public string tohumAdi;
    public float asamaGecisSuresi = 10f;
    public GameObject[] asamalar;

    [Tooltip("Orakla hasat edilince yere düşecek tohum (PickupableTool) prefabı")]
    public GameObject dusecekTohumPrefab;

    [Tooltip("Hasat edildiğinde kaç adet tohum/ürün düşecek?")]
    public int hasatMiktari = 3; // Miktar belirleme alanı eklendi
}

public class SoilTile : NetworkBehaviour, IInteractable
{
    [Header("Toprak Ayarları")]
    public GameObject normalToprakGorseli;
    public GameObject capalanmisToprakGorseli;

    [Header("Ekin Ayarları")]
    public TohumAsamaVerisi[] tohumListesi;

    private NetworkVariable<SoilState> toprakDurumu = new NetworkVariable<SoilState>(SoilState.Normal);
    private NetworkVariable<int> ekiliTohumID = new NetworkVariable<int>(0);
    private NetworkVariable<int> mevcutAsama = new NetworkVariable<int>(0);
    private float buyumeSayaci = 0f;

    // Aletlerin toprağın durumunu okuyabilmesi için public bir erişim
    public SoilState MevcutDurum => toprakDurumu.Value;

    public override void OnNetworkSpawn()
    {
        toprakDurumu.OnValueChanged += OnDurumDegisti;
        ekiliTohumID.OnValueChanged += OnDurumDegisti;
        mevcutAsama.OnValueChanged += OnDurumDegisti;
        GorselleriGuncelle();
    }

    public override void OnNetworkDespawn()
    {
        toprakDurumu.OnValueChanged -= OnDurumDegisti;
        ekiliTohumID.OnValueChanged -= OnDurumDegisti;
        mevcutAsama.OnValueChanged -= OnDurumDegisti;
    }

    // INTERACT MANTIĞI TAMAMEN DEĞİŞTİ (SOLID UYUMLU)
    public void Interact(NetworkObject interactor)
    {
        if (interactor.TryGetComponent(out PlayerInventory inventory))
        {
            // Eğer oyuncunun elinde bir obje varsa ve bu obje IUseableTool arayüzüne sahipse (Yani kullanılabilir bir aletse)
            if (inventory.eldekiObje != null && inventory.eldekiObje.TryGetComponent(out IUseableTool aktifAlet))
            {
                // İşlemi tamamen aletin kendisine devret
                //aktifAlet.EylemYap(this, inventory);
            }
        }
    }

    // ==========================================
    // ALETLERİN TETİKLEYECEĞİ FONKSİYONLAR
    // ==========================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void CapalaServerRpc()
    {
        if (toprakDurumu.Value == SoilState.Normal) toprakDurumu.Value = SoilState.Tilled;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void TohumEkServerRpc(int tohumID)
    {
        if (toprakDurumu.Value == SoilState.Tilled)
        {
            ekiliTohumID.Value = tohumID;
            mevcutAsama.Value = 0;
            buyumeSayaci = 0f;
            toprakDurumu.Value = SoilState.Planted;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SulaServerRpc()
    {
        if (toprakDurumu.Value == SoilState.Planted) toprakDurumu.Value = SoilState.Watered;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void HasatEtServerRpc()
    {
        if (toprakDurumu.Value != SoilState.Grown) return;

        int tohumIndex = ekiliTohumID.Value - 1;
        if (tohumIndex >= 0 && tohumIndex < tohumListesi.Length)
        {
            GameObject dusenPrefab = tohumListesi[tohumIndex].dusecekTohumPrefab;
            int miktar = tohumListesi[tohumIndex].hasatMiktari; // Belirlenen miktarı al

            if (dusenPrefab != null)
            {
                // Inspector'da girilen miktar kadar tohumu etrafa saçarak spawnla
                for (int i = 0; i < miktar; i++)
                {
                    Vector3 rastgeleOffset = new Vector3(Random.Range(-0.5f, 0.5f), 1f, Random.Range(-0.5f, 0.5f));
                    GameObject tohum = Instantiate(dusenPrefab, transform.position + rastgeleOffset, Quaternion.identity);
                    tohum.GetComponent<NetworkObject>().Spawn();
                }
            }
        }

        // Toprağı boş ve çapalanmış duruma sıfırla
        ekiliTohumID.Value = 0;
        mevcutAsama.Value = 0;
        buyumeSayaci = 0f;
        toprakDurumu.Value = SoilState.Tilled;
    }

    // ==========================================
    // BÜYÜME VE GÖRSEL GÜNCELLEME (Değişmedi)
    // ==========================================

    void Update()
    {
        if (!IsServer || toprakDurumu.Value != SoilState.Watered) return;

        if (ekiliTohumID.Value > 0)
        {
            int tohumIndex = ekiliTohumID.Value - 1;
            if (tohumIndex >= 0 && tohumIndex < tohumListesi.Length)
            {
                int maksimumAsama = tohumListesi[tohumIndex].asamalar.Length - 1;
                if (mevcutAsama.Value < maksimumAsama)
                {
                    buyumeSayaci += Time.deltaTime;
                    if (buyumeSayaci >= tohumListesi[tohumIndex].asamaGecisSuresi)
                    {
                        buyumeSayaci = 0f;
                        mevcutAsama.Value++;
                        if (mevcutAsama.Value == maksimumAsama) toprakDurumu.Value = SoilState.Grown;
                    }
                }
            }
        }
    }

    private void OnDurumDegisti<T>(T eskiDeger, T yeniDeger) { GorselleriGuncelle(); }

    private void GorselleriGuncelle()
    {
        if (normalToprakGorseli != null) normalToprakGorseli.SetActive(toprakDurumu.Value == SoilState.Normal);
        if (capalanmisToprakGorseli != null) capalanmisToprakGorseli.SetActive(toprakDurumu.Value != SoilState.Normal);

        for (int i = 0; i < tohumListesi.Length; i++)
        {
            for (int j = 0; j < tohumListesi[i].asamalar.Length; j++)
            {
                if (tohumListesi[i].asamalar[j] != null) tohumListesi[i].asamalar[j].SetActive(false);
            }
        }

        if (ekiliTohumID.Value > 0 && (toprakDurumu.Value == SoilState.Planted || toprakDurumu.Value == SoilState.Watered || toprakDurumu.Value == SoilState.Grown))
        {
            int tohumIndex = ekiliTohumID.Value - 1;
            if (tohumIndex >= 0 && tohumIndex < tohumListesi.Length)
            {
                int asamaIndex = Mathf.Clamp(mevcutAsama.Value, 0, tohumListesi[tohumIndex].asamalar.Length - 1);
                GameObject aktifGorsel = tohumListesi[tohumIndex].asamalar[asamaIndex];
                if (aktifGorsel != null) aktifGorsel.SetActive(true);
            }
        }
    }
}