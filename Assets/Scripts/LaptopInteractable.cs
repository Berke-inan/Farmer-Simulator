using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine; // Yeni Cinemachine namespace'i

public class LaptopInteractable : NetworkBehaviour, IInteractable
{
    [Header("Cinemachine Ayarları")]
    public CinemachineCamera laptopCamera; // Yeni kamera sınıfı adı
    public float blendDuration = 1.5f;

    [Header("UI Sistemi")]
    public MarketUIController marketUI;

    public void Interact(NetworkObject playerNetworkObject)
    {
        if (!playerNetworkObject.IsOwner) return;

        // Laptobun kamerasının önceliğini artırıyoruz. 
        laptopCamera.Priority = 20;

        // Geçiş bitene kadar bekle, sonra UI'ı aç
        StartCoroutine(WaitAndOpenUI());
    }

    private IEnumerator WaitAndOpenUI()
    {
        // Kameranın laptoba tam oturması için geçiş süresi kadar bekliyoruz
        yield return new WaitForSeconds(blendDuration);

        // UI'ı aç ve fareyi serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        marketUI.OpenUI(this);
    }

    public void ExitLaptop()
    {
        // 1. UI'ı kapat
        marketUI.CloseUI();

        // 2. Fareyi tekrar kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 3. Laptop kamerasının önceliğini geri sıfırla. 
        laptopCamera.Priority = 0;
    }
}