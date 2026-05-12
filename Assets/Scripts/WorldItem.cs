using Unity.Netcode;
using UnityEngine;

public class WorldItem : NetworkBehaviour, IInteractable
{
    public ItemData ItemData;
    public int Amount = 1;

    public void Interact(NetworkObject interactor)
    {
        // PlayerInteractor zaten 'is WorldItem' kontrolüyle toplama yapıyor, 
        // burayı boş bırakabilirsin ama silme.
    }

}