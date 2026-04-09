using Unity.Netcode;

public interface IInteractable
{
    // Etkileşime giren oyuncunun NetworkObject'ini parametre olarak alıyoruz
    void Interact(NetworkObject interactor);
}