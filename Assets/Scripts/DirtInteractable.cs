using Unity.Netcode;
using UnityEngine;

public class DirtInteractable : NetworkBehaviour, IInteractable
{
    [Header("Görseller")]
    [Tooltip("Sırasıyla ekin görsellerini atayın. Element 0 = ID 1 (Beyaz), Element 1 = ID 2 (Sarı) vs.")]
    public GameObject[] ekinGorselleri;

    // 0 değeri toprağın boş olduğunu temsil eder
    private NetworkVariable<int> ekiliTohumID = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        ekiliTohumID.OnValueChanged += OnTohumDegisti;
        GorselleriGuncelle(ekiliTohumID.Value);
    }

    public override void OnNetworkDespawn()
    {
        ekiliTohumID.OnValueChanged -= OnTohumDegisti;
    }

    public void Interact(NetworkObject interactor)
    {
        if (ekiliTohumID.Value != 0) return; // Zaten ekili

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
    }

    private void OnTohumDegisti(int eskiDeger, int yeniDeger)
    {
        GorselleriGuncelle(yeniDeger);
    }

    private void GorselleriGuncelle(int tohumID)
    {
        // Önce tüm görselleri gizle
        for (int i = 0; i < ekinGorselleri.Length; i++)
        {
            if (ekinGorselleri[i] != null)
            {
                ekinGorselleri[i].SetActive(false);
            }
        }

        // Eğer tohumID 0'dan büyükse ilgili görseli aç
        if (tohumID > 0)
        {
            // ID'ler 1'den, array indeksleri 0'dan başladığı için (tohumID - 1) yapılıyor.
            int arrayIndex = tohumID - 1;

            // Güvenlik kontrolü: Array sınırları içinde mi?
            if (arrayIndex >= 0 && arrayIndex < ekinGorselleri.Length)
            {
                if (ekinGorselleri[arrayIndex] != null)
                {
                    ekinGorselleri[arrayIndex].SetActive(true);
                }
            }
        }
    }
}