using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour
{
    // 0 = Eli boş, 1 = Beyaz, 2 = Sarı, 3 = Kırmızı vb.
    public int currentSeedID = 0;
}