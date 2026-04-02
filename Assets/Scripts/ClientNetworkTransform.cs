using Unity.Netcode.Components;
using UnityEngine;

// Bu script Unity'nin kendi NetworkTransform sınıfını miras alır
// ve sadece "Sunucu Otoritesi" kuralını iptal eder.
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Yetkiyi Host'tan alıp objenin sahibine (Client'a) veriyoruz
    }
}