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
        // Sunucu deðilsek veya makine kapalýysa iþlem yapma
        if (!NetworkManager.Singleton.IsServer || anaGovde == null || !anaGovde.isWorking.Value) return;

        // ZAMANLAYICIYI SÝLDÝK! Artýk deðdiði her þeyi ayný anda tarayacak.

        // 1. AÐAÇ KONTROLÜ
        TreeController agac = other.GetComponentInParent<TreeController>();
        if (agac != null && !agac.ilaclandiMi.Value)
        {
            agac.IlaclandiServerRpc();
        }

        // 2. NORMAL EKÝN KONTROLÜ
        ModularCrop ekin = other.GetComponentInParent<ModularCrop>();
        if (ekin != null && !ekin.ilaclandiMi.Value)
        {
            ekin.IlaclandiServerRpc();
        }
    }
}