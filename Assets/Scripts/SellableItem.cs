using Unity.Netcode;
using UnityEngine;

public class SellableItem : NetworkBehaviour
{
    public int price; // Eşyanın satış fiyatı
    public string itemName;
}