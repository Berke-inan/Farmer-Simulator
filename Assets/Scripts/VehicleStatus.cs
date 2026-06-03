using UnityEngine;
using Unity.Netcode;

public class VehicleStatus : NetworkBehaviour
{
    private DurabilityManager[] tumParcalar;

    public override void OnNetworkSpawn()
    {
        // Traktörün üzerindeki ve altýndaki tüm parçalarý (Motor + 4 Lastik) bulup hafýzaya alýr
        tumParcalar = GetComponentsInChildren<DurabilityManager>();
    }

    // Traktör hareket kodun bu fonksiyona "Gidebilir miyim?" diye soracak
    public bool SurusIcinUygunMu()
    {
        if (tumParcalar == null || tumParcalar.Length == 0) return true;

        foreach (var parca in tumParcalar)
        {
            // Eðer herhangi bir parçanýn (motor veya lastik) caný sýfýrsa, sürüþe izin verme!
            if (parca.currentHealth.Value <= 0)
            {
                return false;
            }
        }

        return true; // Tüm parçalar saðlamsa gidebilir
    }
}