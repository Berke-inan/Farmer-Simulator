using UnityEngine;

public class OrakEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(SoilTile hedefToprak, PlayerInventory envanter)
    {
        if (hedefToprak.MevcutDurum == SoilState.Grown)
        {
            // Toprağa "Hasat Et ve tohumları düşür" emrini gönder
            hedefToprak.HasatEtServerRpc();
        }
    }
}