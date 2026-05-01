using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(TractorController))]
[RequireComponent(typeof(Rigidbody))] // Hız kontrolü için zorunlu kılındı
public class TractorEquipmentManager : NetworkBehaviour
{
    [Header("Bağlantı Noktaları (Hitch Points)")]
    public Transform rearHitchPoint;  // Römork için (Arka)
    public Transform frontHitchPoint; // Biçer için (Ön)

    [Header("Mesafe Ayarları")]
    [Tooltip("Topuzların birbirine ne kadar yaklaşması gerekiyor?")]
    public float maxAttachDistance = 2.0f;
    public float maxAttachSpeed = 2.0f; // YENİ: Fırlamayı önlemek için hız sınırı

    [Header("Kamera Kaydırma Ayarları")]
    public Transform cameraTarget;
    public float defaultZOffset = 0f;
    public float trailerCameraOffset = -5.0f; // Römork takılınca kaç metre geri gitsin?

    private AttachableEquipment equipmentInRange;
    private ConfigurableJoint currentJoint;
    private TractorController tractorController;
    private Rigidbody rb; // YENİ: Hız kontrolü için eklendi

    private Coroutine cameraCoroutine;

    private void Awake()
    {
        tractorController = GetComponent<TractorController>();
        rb = GetComponent<Rigidbody>(); // YENİ: Atama yapıldı
    }

    private void Update()
    {
        // SADECE TRAKTÖRÜ BEN SÜRÜYORSAM KLAVYEYİ DİNLE
        if (tractorController.IsDrivenByMe)
        {
            // --- F TUŞU: EKİPMAN TAK / ÇIKAR ---
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                // YENİ GÜVENLİK: Traktör çok hızlıysa takma/çıkarma işlemini iptal et
                if (rb.linearVelocity.magnitude > maxAttachSpeed)
                {
                    Debug.LogWarning("Çok hızlı gidiyorsun! Ekipman işlemi iptal edildi. Hızın: " + rb.linearVelocity.magnitude);
                    return;
                }

                if (currentJoint == null && equipmentInRange != null)
                {
                    float distToFront = Vector3.Distance(equipmentInRange.hitchPoint.position, frontHitchPoint.position);
                    float distToRear = Vector3.Distance(equipmentInRange.hitchPoint.position, rearHitchPoint.position);

                    string detectedSide = "";

                    if (distToFront < distToRear && distToFront <= maxAttachDistance)
                    {
                        detectedSide = "Front";
                    }
                    else if (distToRear < distToFront && distToRear <= maxAttachDistance)
                    {
                        detectedSide = "Rear";
                    }

                    if (!string.IsNullOrEmpty(detectedSide))
                    {
                        bool isCorrectSide = false;
                        if (equipmentInRange.type == AttachableEquipment.EquipmentType.Trailer && detectedSide == "Rear") isCorrectSide = true;
                        if (equipmentInRange.type == AttachableEquipment.EquipmentType.Header && detectedSide == "Front") isCorrectSide = true;

                        if (isCorrectSide)
                        {
                            AttachEquipmentServerRpc(equipmentInRange.NetworkObjectId, detectedSide);

                            if (cameraTarget != null && detectedSide == "Rear")
                            {
                                if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
                                cameraCoroutine = StartCoroutine(SmoothCameraMove(trailerCameraOffset));
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Bu ekipman, traktörün bu tarafına takılamaz!");
                        }
                    }
                    else
                    {
                        Debug.Log("Traktörün topuzu, ekipmanın topuzuna yeterince yakın değil! Lütfen daha iyi yanaşın.");
                    }
                }
                else if (currentJoint != null)
                {
                    DetachEquipmentServerRpc();

                    if (cameraTarget != null)
                    {
                        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
                        cameraCoroutine = StartCoroutine(SmoothCameraMove(defaultZOffset));
                    }
                }
            }

            // --- V TUŞU: TAKILI EKİPMANI ÇALIŞTIR / DURDUR ---
            if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
            {
                if (currentJoint != null)
                {
                    AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
                    if (eq != null)
                    {
                        // Traktör, arkasındaki alete "Çalışma durumunu değiştir" komutunu gönderiyor!
                        eq.CalismayiDegistirServerRpc();
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (!tractorController.IsDrivenByMe) return;

        if (currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null)
            {
                eq.GetComponent<Rigidbody>().WakeUp();
                bool isTryingToMove = Mathf.Abs(tractorController.CurrentGasInput) > 0.05f;

                foreach (WheelCollider trailerWC in eq.wheelColliders)
                {
                    if (trailerWC != null)
                    {
                        if (isTryingToMove)
                        {
                            trailerWC.motorTorque = 0.0001f;
                            trailerWC.brakeTorque = 0f;
                        }
                        else
                        {
                            trailerWC.motorTorque = 0f;
                            trailerWC.brakeTorque = 0.5f;
                        }
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        AttachableEquipment eq = other.GetComponentInParent<AttachableEquipment>();
        if (eq != null) equipmentInRange = eq;
    }

    private void OnTriggerExit(Collider other)
    {
        AttachableEquipment eq = other.GetComponentInParent<AttachableEquipment>();
        if (eq != null && equipmentInRange == eq)
        {
            equipmentInRange = null;
        }
    }

    [Rpc(SendTo.Server)]
    private void AttachEquipmentServerRpc(ulong equipmentNetworkId, string side)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(equipmentNetworkId, out NetworkObject netObj))
        {
            AttachableEquipment eq = netObj.GetComponent<AttachableEquipment>();
            Rigidbody eqRb = eq.GetComponent<Rigidbody>();
            Transform targetPoint = (side == "Front") ? frontHitchPoint : rearHitchPoint;

            // 1. Fiziksel Hazırlık: Çarpışmaları kapat
            eqRb.isKinematic = true;
            eqRb.detectCollisions = false;

            // YENİ GÜVENLİK: Makinenin o anki bütün hızını ve savrulmasını sıfırla (Fırlamayı önler)
            eqRb.linearVelocity = Vector3.zero;
            eqRb.angularVelocity = Vector3.zero;

            // ==========================================
            // YENİ EKLENEN: Alet bağlandı, park frenini indir!
            // ==========================================
            eq.ParkFreniniCek(false);

            // SADECE BİÇERDÖVERİ DÖNDÜR (Orijinal çalışan mantığın)
            if (eq.type == AttachableEquipment.EquipmentType.Header)
            {
                eq.transform.rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
            }
            // DİĞERLERİ (Pulluk/Römork) KENDİ DOĞAL AÇISINDA KALACAK

            // Pozisyonu ucu ucuna çek
            eq.transform.position = targetPoint.position - (eq.hitchPoint.position - eq.transform.position);
            Physics.SyncTransforms();

            // BAĞLANTI (JOINT) OLUŞTURMA 
            ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = eqRb;
            joint.anchor = transform.InverseTransformPoint(targetPoint.position);

            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;

            if (eq.type == AttachableEquipment.EquipmentType.Header)
            {
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit highXLimit = new SoftJointLimit { limit = 20f };
                SoftJointLimit lowXLimit = new SoftJointLimit { limit = -20f };
                joint.highAngularXLimit = highXLimit;
                joint.lowAngularXLimit = lowXLimit;
            }
            else
            {
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                // Pulluğun traktörün içine girmesini engelleyen 50 derecelik güvenli sınırın duruyor
                joint.angularYMotion = ConfigurableJointMotion.Limited;

                SoftJointLimit angularYLimit = new SoftJointLimit { limit = 50f };
                joint.angularYLimit = angularYLimit;
            }

            // Traktör ve aletin birbirine çarpıp uçmasını engelle
            IgnoreCollisions(eq.gameObject, true);

            // Sistemi normale döndür
            eqRb.detectCollisions = true;
            eqRb.isKinematic = false;

            currentJoint = joint;
            eqRb.WakeUp();
        }
    }

    [Rpc(SendTo.Server)]
    private void DetachEquipmentServerRpc()
    {
        if (currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null)
            {
                // Aleti traktörden çıkardığın an makinenin çalışması (isWorking) otomatik dursun
                eq.isWorking.Value = false;

                // ==========================================
                // YENİ EKLENEN: Alet çıkarıldı, el frenini çek!
                // ==========================================
                eq.ParkFreniniCek(true);

                // YENİ GÜVENLİK: Çarpışmaları anında açma, 2 saniye bekle ki fırlamasın
                StartCoroutine(GuvenliCarpismaAc(eq.gameObject));
            }

            Destroy(currentJoint);
            currentJoint = null;
        }
    }

    // YENİ GÜVENLİK: Çıkarılan aletin çarpışmalarını 2 saniye sonra açan sistem
    private System.Collections.IEnumerator GuvenliCarpismaAc(GameObject equipment)
    {
        yield return new WaitForSeconds(2f);
        if (equipment != null)
        {
            IgnoreCollisions(equipment, false);
        }
    }

    private void IgnoreCollisions(GameObject equipment, bool ignore)
    {
        Collider[] tractorColliders = GetComponentsInChildren<Collider>();
        Collider[] equipmentColliders = equipment.GetComponentsInChildren<Collider>();

        foreach (Collider tCol in tractorColliders)
        {
            foreach (Collider eCol in equipmentColliders)
            {
                Physics.IgnoreCollision(tCol, eCol, ignore);
            }
        }
    }

    private System.Collections.IEnumerator SmoothCameraMove(float targetZ)
    {
        Vector3 targetPos = new Vector3(cameraTarget.localPosition.x, cameraTarget.localPosition.y, targetZ);

        while (Vector3.Distance(cameraTarget.localPosition, targetPos) > 0.01f)
        {
            cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, targetPos, Time.deltaTime * 4f);
            yield return null;
        }
        cameraTarget.localPosition = targetPos;
    }
}