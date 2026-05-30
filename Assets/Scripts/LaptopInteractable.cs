using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class LaptopInteractable : NetworkBehaviour, IInteractable
{
    [Header("Cinemachine Ayarları")]
    public CinemachineCamera laptopCamera;
    public CinemachineCamera topDownArsaCamera;
    public float blendDuration = 1.5f;

    [Header("UI Sistemi")]
    public MarketUIController marketUI;

    [Header("Arsa Sistemi")]
    public ArsaSecici arsaSecici;

    private NetworkVariable<bool> isBusy = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool arsaSecimModunda = false;

    private NetworkObject aktifOyuncu;

    public void Interact(NetworkObject playerNetworkObject)
    {
        if (isBusy.Value) return;
        if (!playerNetworkObject.IsOwner) return;

        SetLaptopBusyServerRpc(true);
        aktifOyuncu = playerNetworkObject;

        // --- KESİN ÇÖZÜM: Senin PlayerMovement kodunu kapatıyoruz ---
        // Senin kodundaki OnDisable() metodu çalıştığı an karakterin hızı sıfırlanıp kilitlenecek!
        PlayerMovement yurumeKodu = aktifOyuncu.GetComponent<PlayerMovement>();
        if (yurumeKodu != null)
        {
            yurumeKodu.enabled = false;
        }

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

    private void Update()
    {
        if (arsaSecimModunda && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ArsaSecimindenCik();
        }
    }

    public void ArsaSecimModunaGec()
    {
        marketUI.CloseUI();
        laptopCamera.Priority = 0;
        topDownArsaCamera.Priority = 20;
        arsaSecici.SecimModunuAc();
        arsaSecimModunda = true;
    }

    public void ArsaSecimindenCik()
    {
        arsaSecimModunda = false;
        arsaSecici.SecimModunuKapat();
        topDownArsaCamera.Priority = 0;
        laptopCamera.Priority = 20;
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

        SetLaptopBusyServerRpc(false);

        // --- HAREKETİ GERİ AÇ ---
        // Çıkış yaptığımızda kodun OnEnable() metodu çalışıp inputları geri yükleyecek
        if (aktifOyuncu != null)
        {
            PlayerMovement yurumeKodu = aktifOyuncu.GetComponent<PlayerMovement>();
            if (yurumeKodu != null)
            {
                yurumeKodu.enabled = true;
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SetLaptopBusyServerRpc(bool busyStatus)
    {
        isBusy.Value = busyStatus;
    }
}