using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgirlikMerkeziAyarla : MonoBehaviour
{
    [Tooltip("Traktörün/Pulluðun altýna yerleþtirdiðimiz boþ AgirlikMerkezi objesini buraya sürükleyin")]
    public Transform agirlikMerkeziObjesi;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        MerkeziUygula();
    }

    // Unity Editöründe oyunu durdurmadan kýrmýzý topu hareket ettirip deneme yapabilmen için Update'e de ekledik
    private void Update()
    {
        MerkeziUygula();
    }

    private void MerkeziUygula()
    {
        if (agirlikMerkeziObjesi != null)
        {
            // ÝÞTE SÝHÝRLÝ KOD BU: Obje nerede olursa olsun, Rigidbody'ye göre gerçek yerini hesaplar
            rb.centerOfMass = transform.InverseTransformPoint(agirlikMerkeziObjesi.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (agirlikMerkeziObjesi != null)
        {
            // Kýrmýzý topu doðrudan objenin olduðu yere çiziyoruz
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(agirlikMerkeziObjesi.position, 0.3f);
        }
    }
}