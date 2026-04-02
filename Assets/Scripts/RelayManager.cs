using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else { Instance = this; DontDestroyOnLoad(gameObject); }
    }

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (System.Exception e) { Debug.LogError("Services Hatası: " + e.Message); }
    }

    // HOST İÇİN: Kodu alır ve oyunu ANINDA başlatır
    public async Task<string> SetupAndStartRelay()
    {
        try
        {
            // 1. Sunucuda yerini ayır (3 arkadaşın için + sen)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);

            // 2. Kodu üret
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 3. Transport ayarlarını göm (Unity 6 / UTP 2.0+ uyumlu)
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 4. KRİTİK: Oyunu hemen başlatıyoruz. Devam butonunu beklemiyoruz!
            NetworkManager.Singleton.StartHost();

            Debug.Log("Oyun Arkada Başlatıldı! Kod: " + joinCode);
            return joinCode;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Kurulum Hatası: " + e.Message);
            return null;
        }
    }

    // CLIENT İÇİN: Kodla direkt katılır
    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e) { Debug.LogError("Bağlantı Hatası: " + e.Message); }
    }
}