using Unity.Netcode;
using UnityEngine;

public class WorldItem : NetworkBehaviour
{
    public ItemData ItemData;
    public int Amount = 1;
}