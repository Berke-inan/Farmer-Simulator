using UnityEngine;
using Unity.Netcode;

public class IlaclamaMakinesi : MonoBehaviour
{
    [Header("Baðlantýlar")]
    public AttachableEquipment anaGovde;

    [Header("Görsel Efektler")]
    public ParticleSystem puskurtmeEfektiSol;
    public ParticleSystem puskurtmeEfektiSag;

    private void Update()
    {
        if (anaGovde == null) return;

        bool calisiyorMu = anaGovde.isWorking.Value;

        // Efektleri kontrol et
        ToggleEffect(puskurtmeEfektiSol, calisiyorMu);
        ToggleEffect(puskurtmeEfektiSag, calisiyorMu);
    }

    private void ToggleEffect(ParticleSystem ps, bool state)
    {
        if (ps == null) return;
        if (state && !ps.isPlaying) ps.Play();
        else if (!state && ps.isPlaying) ps.Stop();
    }

    private void OnTriggerStay(Collider other)
    {
        // 1. Önce NetworkManager'ýn varlýðýný ve Server olup olmadýðýný kontrol et
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        // 2. anaGovde referansýný ve NetworkObject'in spawn durumunu kontrol et
        if (anaGovde == null || !anaGovde.IsSpawned || !anaGovde.isWorking.Value) return;

        // 3. AÐAÇ KONTROLÜ
        TreeController agac = other.GetComponentInParent<TreeController>();
        if (agac != null && agac.ilaclandiMi != null && !agac.ilaclandiMi.Value)
        {
            agac.IlaclandiServerRpc();
        }

        // 4. EKÝN KONTROLÜ
        ModularCrop ekin = other.GetComponentInParent<ModularCrop>();
        if (ekin != null && ekin.ilaclandiMi != null && !ekin.ilaclandiMi.Value)
        {
            ekin.IlaclandiServerRpc();
        }
    }
}