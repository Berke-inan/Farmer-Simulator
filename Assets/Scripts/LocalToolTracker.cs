using UnityEngine;

public class LocalToolTracker : MonoBehaviour
{
    [Header("Takip Ayarları")]
    public Vector3 offset = new Vector3(0.5f, -0.4f, 1f);
    public Vector3 rotationOffset = Vector3.zero; // Aletin yamuk durmasını düzeltecek açı
    public float followSpeed = 10f;

    private Transform targetTransform;

    // Alet ilk ele alındığında anında hedefe ışınlanması için
    public void Setup(Transform target)
    {
        targetTransform = target;

        // Obje doğduğu an saçma bir yerde gözükmesin diye anında yerine oturtuyoruz
        transform.position = targetTransform.position + targetTransform.TransformDirection(offset);
        transform.rotation = targetTransform.rotation * Quaternion.Euler(rotationOffset);
    }

    void Update()
    {
        if (targetTransform == null) return;

        // Eski sistemindeki matematiğin aynısı
        Vector3 targetPos = targetTransform.position + targetTransform.TransformDirection(offset);
        Quaternion targetRot = targetTransform.rotation * Quaternion.Euler(rotationOffset);

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
    }
}