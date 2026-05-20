using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace FarmerSimulator.Network
{
    public class RelayManager : MonoBehaviour
    {
        public static RelayManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Services Hatası: " + e.Message);
            }
        }

        /// <summary>
        /// HOST İÇİN: Kodu alır ve oyunu anında başlatır.
        /// </summary>
        public async Task<string> SetupAndStartRelay(int maxConnections = 3)
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

                // Unity 6 UTP 2.0+ uyumlu veri aktarımı
                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData
                );

                NetworkManager.Singleton.StartHost();
                Debug.Log("Oyun Arkada Başlatıldı! Kod: " + joinCode);
                return joinCode;
            }
            catch (Exception e)
            {
                Debug.LogError("Relay Kurulum Hatası: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// CLIENT İÇİN: Kodla katılır ve UI'a durumu bildirir.
        /// </summary>
        public async Task<bool> JoinRelay(string joinCode)
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

                return NetworkManager.Singleton.StartClient();
            }
            catch (Exception e)
            {
                Debug.LogError("Bağlantı Hatası: " + e.Message);
                return false;
            }
        }
    }
}