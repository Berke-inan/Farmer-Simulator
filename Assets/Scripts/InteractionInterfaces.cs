using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Yerden eşya alma, kapı açma, traktöre binme gibi temel "E" tuşu etkileşimleri için.
public interface IInteractable
{
    void Interact(NetworkObject user);

    List<ActionPrompt> GetPrompts();
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


public struct ActionPrompt
{
    public string Key;
    public string Action;

    public ActionPrompt(string key, string action)
    {
        Key = key;
        Action = action;
    }
}