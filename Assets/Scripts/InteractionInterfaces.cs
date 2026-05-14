using Unity.Netcode;
using UnityEngine;

// Yerden eşya alma, kapı açma, traktöre binme gibi temel "E" tuşu etkileşimleri için.
public interface IInteractable
{
    void Interact(NetworkObject user);
}

// Balyalama, sandık açma gibi ikincil "F" tuşu etkileşimleri için.
public interface ISecondaryInteractable
{
    void SecondaryInteract(NetworkObject user);
}

// Elde tutulan çapa, tohum, sulama kabı gibi aletlerin sol tık (Attack) ile kullanımı için.
public interface IUseableTool
{
    void EylemYap(RaycastHit hit, PlayerInventory inventory);
}