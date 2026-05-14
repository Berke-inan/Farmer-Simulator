using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class SepetEsyasi : MonoBehaviour
{
    [Header("Sepet Modelleri")]
    public GameObject modelBos;
    public GameObject modelYarim;
    public GameObject modelDolu;

    [Header("Toplama Ayarlarý")]
    public float toplamaMenzili = 4f;

    private PlayerInventory inventory;

    private void Start()
    {
        // Obje eline verildiðinde (Instantiate edildiðinde) oyuncunun envanter koduna ulaþýr
        inventory = GetComponentInParent<PlayerInventory>();

        if (inventory != null)
        {
            // Sepetin ilk doluluk durumuna göre görseli ayarla
            GorselleriGuncelle(inventory.sepetDoluluk.Value);

            // Doluluk deðiþtiðinde (NetworkVariable) görselleri güncellemesi için abone ol
            inventory.sepetDoluluk.OnValueChanged += DolulukDegistigindeGorselleriGuncelle;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.sepetDoluluk.OnValueChanged -= DolulukDegistigindeGorselleriGuncelle;
        }
    }

    private void DolulukDegistigindeGorselleriGuncelle(int eskiDurum, int yeniDurum)
    {
        GorselleriGuncelle(yeniDurum);
    }

    private void GorselleriGuncelle(int durum)
    {
        if (modelBos != null) modelBos.SetActive(durum == 0);
        if (modelYarim != null) modelYarim.SetActive(durum == 1);
        if (modelDolu != null) modelDolu.SetActive(durum >= 2);
    }

    private void Update()
    {
        // Sadece sahibi bizsek ve F tuþuna basýldýysa
        if (inventory == null || !inventory.IsOwner) return;

        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (inventory.sepetDoluluk.Value >= 2)
            {
                Debug.Log("Sepet tamamen dolu!");
                return;
            }

            // PlayerInteractor üzerinden kameraya ulaþýrýz
            Transform cam = inventory.GetComponent<PlayerInteractor>().playerCamera;
            Ray ray = new Ray(cam.position, cam.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, toplamaMenzili))
            {
                TreeController agac = hit.collider.GetComponentInParent<TreeController>();
                if (agac != null && agac.mevcutDurum.Value == TreeState.Meyveli)
                {
                    // RPC artýk PlayerInventory üzerinden gönderilmeli
                    inventory.ToplamaIstegiServerRpc(agac.NetworkObjectId);
                }
            }
        }
    }
}