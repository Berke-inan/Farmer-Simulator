using UnityEngine;
using Unity.Netcode;

public class BreakableTarget : NetworkBehaviour
{
    [Header("Fiziksel Parçalanma Ayarlarý")]
    public float patlamaGucu = 1000f;
    public float patlamaYaricapi = 3f;

    [Header("Ses Ayarlarý")]
    [Tooltip("Ayarlarýný yaptýðýn AudioSource bileþenini taþýyan ALT OBJEYÝ buraya sürükle")]
    public AudioSource breakAudioSource;

    private bool isBroken = false;

    public void TakeDamage()
    {
        if (isBroken) return;
        BreakServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void BreakServerRpc()
    {
        if (isBroken) return;
        isBroken = true;

        ShatterClientRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ShatterClientRpc()
    {
        // --- YENÝ SES SÝSTEMÝ (INSPECTOR KONTROLLÜ) ---
        if (breakAudioSource != null)
        {
            // 1. Ses objesini ana bardaktan tamamen koparýp baðýmsýz yapýyoruz (Bardak silinince ses kesilmesin)
            breakAudioSource.transform.SetParent(null);

            // 2. Makinalý tüfekle tarandýðýnda seslerin üst üste binmemesi için ufak ton farklýlýðý
            breakAudioSource.pitch = Random.Range(0.85f, 1.15f);

            // 3. Sesi çal
            breakAudioSource.Play();

            // 4. Klip uzunluðunu hesapla ve o süre dolduðunda bu baðýmsýz ses objesini de sahneden temizle
            float klipSuresi = breakAudioSource.clip != null ? breakAudioSource.clip.length : 2f;
            Destroy(breakAudioSource.gameObject, klipSuresi + 0.1f);
        }

        // --- DERÝN ARAMA VE PARÇALANMA ---
        MeshRenderer[] butunParcalar = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer mr in butunParcalar)
        {
            GameObject parca = mr.gameObject;
            parca.transform.SetParent(null);

            if (!parca.TryGetComponent<Collider>(out _))
            {
                parca.AddComponent<BoxCollider>();
            }

            if (!parca.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb = parca.AddComponent<Rigidbody>();
            }

            rb.AddExplosionForce(patlamaGucu, transform.position, patlamaYaricapi);
            Destroy(parca, Random.Range(5f, 8f));
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}