using Unity.Netcode;

public interface ISecondaryInteractable
{
    // F tuşuna basıldığında tetiklenecek fonksiyon
    void SecondaryInteract(NetworkObject interactor);
}