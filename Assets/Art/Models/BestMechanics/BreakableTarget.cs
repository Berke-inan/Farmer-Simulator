using UnityEngine;
using Unity.Netcode;

public class BreakableTarget : NetworkBehaviour
{
    [Header("Fiziksel Parçalanma Ayarlarý")]
    public float patlamaGucu = 1000f;
    public float patlamaYaricapi = 3f;
    public AudioClip breakSound;

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
        // --- %100 ÇALIÞAN HAYALET SES SÝSTEMÝ ---
        if (breakSound != null)
        {
            GameObject sesObjesi = new GameObject("CamKirilmaSesi");
            sesObjesi.transform.position = transform.position;

            AudioSource geciciKaynak = sesObjesi.AddComponent<AudioSource>();
            geciciKaynak.clip = breakSound;

            // spatialBlend: 0 olursa her yerden ayný þiddette duyulur (2D), 1 olursa uzaklaþtýkça azalýr (3D).
            // 0.5f yaparak hem yönünü belli edip hem de sesin kaybolmamasýný saðlýyoruz!
            geciciKaynak.spatialBlend = 0.5f;
            geciciKaynak.volume = 1f;

            // Makinalý tüfekle art arda tarandýðýnda seslerin birbirine girmemesi için ufak ton farklýlýklarý
            geciciKaynak.pitch = Random.Range(0.85f, 1.15f);

            geciciKaynak.Play();

            // Ses dosyasýnýn uzunluðu kadar bekleyip bu hayalet objeyi sahneden temizle
            Destroy(sesObjesi, breakSound.length + 0.1f);
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