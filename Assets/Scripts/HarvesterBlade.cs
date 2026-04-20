using UnityEngine;
using Unity.Netcode;

public class HarvesterBlade : NetworkBehaviour
{
    [Header("Dönüþ Ayarlarý")]
    [Tooltip("Býçaðýn hangi eksende döneceðini belirler. Genelde X (1,0,0) veya Z (0,0,1) olur.")]
    public Vector3 rotationAxis = new Vector3(1, 0, 0);
    public float rotationSpeed = 300f;

    [Header("Að Senkronizasyonu")]
    // Sadece sunucunun (traktördeki sistemin) deðiþtirebileceði, herkesin (diðer oyuncularýn) görebileceði deðiþken
    public NetworkVariable<bool> isSpinning = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Update()
    {
        // Eðer traktörden "Dönmeye baþla" (True) emri geldiyse, býçaðý kendi ekseni etrafýnda çevir
        if (isSpinning.Value)
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}