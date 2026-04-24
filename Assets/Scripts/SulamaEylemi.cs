using UnityEngine;

public class SulamaEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(SoilTile hedefToprak, PlayerInventory envanter)
    {
        if (hedefToprak.MevcutDurum == SoilState.Planted)
        {
            hedefToprak.SulaServerRpc();
        }
    }
}