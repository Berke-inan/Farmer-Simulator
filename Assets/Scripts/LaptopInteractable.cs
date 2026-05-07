using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class LaptopInteractable : NetworkBehaviour, IInteractable
{
    [Header("Cinemachine Ayarları")]
    public CinemachineCamera laptopCamera;
    public float blendDuration = 1.5f;

    [Header("UI Sistemi")]
    public MarketUIController marketUI;

    // Laptobun kullanım durumunu tüm oyunculara senkronize eden değişken
    // WritePermission.Server sayesinde sadece sunucu bu değeri değiştirebilir (güvenli yöntem)
    private NetworkVariable<bool> isBusy = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void Interact(NetworkObject playerNetworkObject)
    {
        // Eğer laptop zaten bir başkası tarafından kullanılıyorsa etkileşimi reddet
        if (isBusy.Value)
        {
            Debug.Log("Laptop şu an meşgul!");
            return;
        }

        // Sadece sahibi (Owner) işlemleri başlatır ama meşguliyet bilgisini sunucuya bildirir
        if (!playerNetworkObject.IsOwner) return;

        // Sunucudan laptobu "meşgul" olarak işaretlemesini istiyoruz
        SetLaptopBusyServerRpc(true);

        laptopCamera.Priority = 20;
        StartCoroutine(WaitAndOpenUI());
    }

    private IEnumerator WaitAndOpenUI()
    {
        yield return new WaitForSeconds(blendDuration);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        marketUI.OpenUI(this);
    }

    public void ExitLaptop()
    {
        marketUI.CloseUI();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        laptopCamera.Priority = 0;

        // Çıkış yaparken laptobu tekrar "erişilebilir" hale getir
        SetLaptopBusyServerRpc(false);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetLaptopBusyServerRpc(bool busyStatus)
    {
        isBusy.Value = busyStatus;
    }
}