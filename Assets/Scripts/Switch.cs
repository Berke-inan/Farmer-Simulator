using UnityEngine;
using Unity.Netcode;

public class Switch : NetworkBehaviour, IInteractable
{
    public Light[] lights;
    private bool isOn;

    public void Interact(NetworkObject interactor)
    {
        isOn = !isOn;

        foreach (Light l in lights)
        {
            l.enabled = isOn;
        }

        Debug.Log("Switch çalýþtý");
    }
}