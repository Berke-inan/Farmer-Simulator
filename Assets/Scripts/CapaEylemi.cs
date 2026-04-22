using UnityEngine;

public class CapaEylemi : MonoBehaviour, IUseableTool
{
    public void EylemYap(SoilTile hedefToprak, PlayerInventory envanter)
    {
        if (hedefToprak.MevcutDurum == SoilState.Normal)
        {
            hedefToprak.CapalaServerRpc();
        }
    }
}