using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

public class RadioSystem : NetworkBehaviour, ISecondaryInteractable
{
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Songs")]
    public AudioClip[] songs;

    [Header("Effects")]
    public AudioClip radioStatic;

    [Header("Settings")]
    public float volumeStep = 0.05f;

    // NETWORK VARIABLES
    private NetworkVariable<bool> isOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> currentSongIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Þarkýnýn baþladýðý server zamaný
    private NetworkVariable<double> songStartTime = new NetworkVariable<double>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isSwitching = false;

    private void Start()
    {
        musicSource.playOnAwake = false;
        musicSource.loop = false;

        isOn.OnValueChanged += OnRadioStateChanged;
        currentSongIndex.OnValueChanged += OnSongChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Sonradan giren oyuncular için senkron
        if (isOn.Value)
        {
            SyncCurrentSong();
        }
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        // SADECE SERVER þarký bitimini kontrol eder
        if (IsServer && isOn.Value && !musicSource.isPlaying && !isSwitching)
        {
            NextSong();
        }

        // SES AYARI
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;

            if (scroll != 0)
            {
                HandleVolume(scroll);
            }
        }

        // R TUÞU ÝLE ÞARKI DEÐÝÞTÝR
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            RequestNextSongServerRpc();
        }
    }

    // F TUÞU ETKÝLEÞÝMÝ
    public void SecondaryInteract(NetworkObject interactor)
    {
        ToggleRadioServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ToggleRadioServerRpc()
    {
        isOn.Value = !isOn.Value;

        if (isOn.Value)
        {
            StartSong(currentSongIndex.Value);
        }
        else
        {
            StopRadioClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestNextSongServerRpc()
    {
        if (!isOn.Value || isSwitching)
            return;

        StartCoroutine(ChangeSongRoutine());
    }

    private IEnumerator ChangeSongRoutine()
    {
        isSwitching = true;

        PlayStaticClientRpc();

        yield return new WaitForSeconds(1f);

        NextSong();

        isSwitching = false;
    }

    private void NextSong()
    {
        int nextIndex = currentSongIndex.Value + 1;

        if (nextIndex >= songs.Length)
        {
            nextIndex = 0;
        }

        currentSongIndex.Value = nextIndex;

        StartSong(nextIndex);
    }

    private void StartSong(int songIndex)
    {
        if (songs.Length == 0)
            return;

        // SERVER SAATÝNÝ KAYDET
        songStartTime.Value = NetworkManager.ServerTime.Time;

        PlaySongClientRpc(songIndex, songStartTime.Value);
    }

    [ClientRpc]
    private void PlaySongClientRpc(int songIndex, double serverStart)
    {
        if (songs.Length == 0)
            return;

        musicSource.Stop();

        musicSource.clip = songs[songIndex];

        // Serverdan beri geçen süre
        double passedTime =
            NetworkManager.Singleton.ServerTime.Time - serverStart;

        float currentTime = (float)passedTime;

        // Eðer þarký hala devam ediyorsa
        if (currentTime < musicSource.clip.length)
        {
            musicSource.time = currentTime;
        }

        musicSource.Play();
    }

    [ClientRpc]
    private void StopRadioClientRpc()
    {
        musicSource.Stop();
    }

    [ClientRpc]
    private void PlayStaticClientRpc()
    {
        musicSource.Stop();

        if (radioStatic != null)
        {
            sfxSource.PlayOneShot(radioStatic);
        }
    }

    private void OnRadioStateChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
        {
            musicSource.Stop();
        }
    }

    private void OnSongChanged(int oldValue, int newValue)
    {
        if (isOn.Value)
        {
            SyncCurrentSong();
        }
    }

    private void SyncCurrentSong()
    {
        if (songs.Length == 0)
            return;

        musicSource.clip = songs[currentSongIndex.Value];

        double passedTime =
            NetworkManager.Singleton.ServerTime.Time - songStartTime.Value;

        float currentTime = (float)passedTime;

        if (currentTime >= musicSource.clip.length)
            return;

        musicSource.time = currentTime;

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void HandleVolume(float scroll)
    {
        if (scroll > 0)
        {
            musicSource.volume += volumeStep;
        }
        else if (scroll < 0)
        {
            musicSource.volume -= volumeStep;
        }

        musicSource.volume = Mathf.Clamp01(musicSource.volume);
    }
}