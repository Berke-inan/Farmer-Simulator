using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class TopDownKameraKontrol : MonoBehaviour
{
    [Header("Hareket Ayarlarý")]
    public float hareketHizi = 25f;
    [Tooltip("Fare tekerleði ile ne kadar hýzlý yakýnlaþsýn?")]
    public float zoomHizi = 3f;

    [Header("Gezinme Sýnýrlarý (Çiftlik Merkezine Göre)")]
    [Tooltip("Kamera X ekseninde (Sað-Sol) ne kadar uzaða gidebilsin?")]
    public Vector2 xSinirlari = new Vector2(-50f, 50f);
    [Tooltip("Kamera Z ekseninde (Ýleri-Geri) ne kadar uzaða gidebilsin?")]
    public Vector2 zSinirlari = new Vector2(-50f, 50f);

    [Header("Zoom Sýnýrlarý (Y Ekseni)")]
    [Tooltip("Yere en fazla ne kadar yaklaþabilsin?")]
    public float minYukseklik = 15f;
    [Tooltip("Gökyüzüne en fazla ne kadar uzaklaþabilsin?")]
    public float maxYukseklik = 70f;

    private CinemachineCamera sanalKamera;

    private void Awake()
    {
        sanalKamera = GetComponent<CinemachineCamera>();
        if (sanalKamera == null)
        {
            Debug.LogError("Bu script sadece CinemachineCamera objesinde çalýþýr!");
        }
    }

    private void Update()
    {
        // SÝHÝRLÝ KONTROL: Eðer laptop bu kamerayý aktif etmediyse (Priority 10'dan düþükse) hiç çalýþma!
        if (sanalKamera == null || sanalKamera.Priority < 10) return;
        if (Keyboard.current == null || Mouse.current == null) return;

        HareketEt();
        ZoomYap();
    }

    private void HareketEt()
    {
        Vector3 hareket = Vector3.zero;

        // WASD Tuþlarý ile yön belirleme
        if (Keyboard.current.wKey.isPressed) hareket += Vector3.forward; // Ýleri
        if (Keyboard.current.sKey.isPressed) hareket += Vector3.back;    // Geri
        if (Keyboard.current.aKey.isPressed) hareket += Vector3.left;    // Sol
        if (Keyboard.current.dKey.isPressed) hareket += Vector3.right;   // Sað

        if (hareket.sqrMagnitude > 0)
        {
            // Kamerayý hareket ettir
            transform.position += hareket.normalized * (hareketHizi * Time.deltaTime);
        }

        // --- SINIR KONTROLÜ (Görünmez Duvarlar) ---
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, xSinirlari.x, xSinirlari.y);
        pos.z = Mathf.Clamp(pos.z, zSinirlari.x, zSinirlari.y);
        transform.position = pos;
    }

    private void ZoomYap()
    {
        // Fare tekerleðinin (Scroll) dönüþ deðerini al
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.1f)
        {
            Vector3 pos = transform.position;

            // Scroll yönüne göre kamerayý aþaðý (yaklaþ) veya yukarý (uzaklaþ) it
            pos.y -= Mathf.Sign(scroll) * zoomHizi;

            // --- ZOOM SINIR KONTROLÜ ---
            pos.y = Mathf.Clamp(pos.y, minYukseklik, maxYukseklik);
            transform.position = pos;
        }
    }
}