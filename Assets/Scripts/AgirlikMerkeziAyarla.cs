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

    private void Update()
    {
        MerkeziUygula();
    }

    private void MerkeziUygula()
    {
        if (agirlikMerkeziObjesi != null)
        {
            rb.centerOfMass = transform.InverseTransformPoint(agirlikMerkeziObjesi.position);
        }
    }

    private void OnDrawGizmos()
    {
        if (agirlikMerkeziObjesi != null)
        {

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(agirlikMerkeziObjesi.position, 0.3f);
        }
    }
}