using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(TractorController))]
public class TractorEquipmentManager : NetworkBehaviour
{
    [Header("Bağlantı Noktaları (Hitch Points)")]
    public Transform rearHitchPoint;  // Römork için (Arka)
    public Transform frontHitchPoint; // Biçer için (Ön)

    // ==========================================
    // YENİ EKLENEN: GÜVENLİ TAKMA MESAFESİ
    // ==========================================
    [Header("Mesafe Ayarları")]
    [Tooltip("Topuzların birbirine ne kadar yaklaşması gerekiyor?")]
    public float maxAttachDistance = 2.0f;

    // ==========================================
    // KAMERA HEDEFİ VE MESAFELERİ
    // ==========================================
    [Header("Kamera Kaydırma Ayarları")]
    public Transform cameraTarget;
    public float defaultZOffset = 0f;
    public float trailerCameraOffset = -5.0f; // Römork takılınca kaç metre geri gitsin?

    private AttachableEquipment equipmentInRange;
    private ConfigurableJoint currentJoint;
    private TractorController tractorController;

    private Coroutine cameraCoroutine;

    private void Awake()
    {
        tractorController = GetComponent<TractorController>();
    }

    private void Update()
    {
        if (tractorController.IsDrivenByMe)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (currentJoint == null && equipmentInRange != null)
                {
                    float distToFront = Vector3.Distance(equipmentInRange.hitchPoint.position, frontHitchPoint.position);
                    float distToRear = Vector3.Distance(equipmentInRange.hitchPoint.position, rearHitchPoint.position);

                    string detectedSide = "";

                    // =======================================================
                    // YENİ MANTIK: Sadece "maxAttachDistance" sınırındaysa
                    // ve hangisi daha yakınsa onu seç.
                    // =======================================================
                    if (distToFront < distToRear && distToFront <= maxAttachDistance)
                    {
                        detectedSide = "Front";
                    }
                    else if (distToRear < distToFront && distToRear <= maxAttachDistance)
                    {
                        detectedSide = "Rear";
                    }

                    // Eğer topuzlardan biri yeterince yakınsa işlemlere devam et
                    if (!string.IsNullOrEmpty(detectedSide))
                    {
                        bool isCorrectSide = false;
                        if (equipmentInRange.type == AttachableEquipment.EquipmentType.Trailer && detectedSide == "Rear") isCorrectSide = true;
                        if (equipmentInRange.type == AttachableEquipment.EquipmentType.Header && detectedSide == "Front") isCorrectSide = true;

                        if (isCorrectSide)
                        {
                            AttachEquipmentServerRpc(equipmentInRange.NetworkObjectId, detectedSide);

                            // RÖMORK TAKILINCA KAMERAYI GERİ ÇEK
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

                    // ALET ÇIKINCA KAMERAYI ESKİ YERİNE AL
                    if (cameraTarget != null)
                    {
                        if (cameraCoroutine != null) StopCoroutine(cameraCoroutine);
                        cameraCoroutine = StartCoroutine(SmoothCameraMove(defaultZOffset));
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsServer && currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null)
            {
                HarvesterBlade blade = eq.GetComponentInChildren<HarvesterBlade>();
                if (blade != null)
                {
                    if (blade.isSpinning.Value != tractorController.IsOccupied)
                    {
                        blade.isSpinning.Value = tractorController.IsOccupied;
                    }
                }
            }
        }

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

            // 1. Fiziksel Hazırlık: Çarpışmaları kapat ve hızı sıfırla (Fırlamayı önler)
            eqRb.isKinematic = true;
            eqRb.detectCollisions = false;

            // ==========================================
            // KESİN ÇÖZÜM: BİÇERDÖVERİ DÜMDÜZ HİZALA
            // ==========================================
            // Sadece Biçerdöver (Header) tipindeyse rotasyonu zorla.
            // Bu sayede yamuk yanaşsan bile alet traktörle paralel hale gelir.
            if (eq.type == AttachableEquipment.EquipmentType.Header)
            {
                // Traktörün açısını alır ve biçerdöveri 180 derece ters çevirip kusursuz hizalar.
                eq.transform.rotation = transform.rotation * Quaternion.Euler(0, 180f, 0);
            }

            // 2. Pozisyonu ucu ucuna çek (Hitch noktalarını eşitle)
            eq.transform.position = targetPoint.position - (eq.hitchPoint.position - eq.transform.position);

            // Yapılan transform değişikliklerini fizik motoruna bildir
            Physics.SyncTransforms();

            // 3. BAĞLANTI (JOINT) OLUŞTURMA
            ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = eqRb;
            joint.anchor = transform.InverseTransformPoint(targetPoint.position);

            // İleri-geri, sağa-sola ve yukarı-aşağı kopmaları engelle (Tam kilit)
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;

            if (eq.type == AttachableEquipment.EquipmentType.Header)
            {
                // BİÇERDÖVER AYARI: Sağa sola dönüş ve yan yatma tamamen kilitli
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;

                // Sadece engebeli arazide esnemesi için yukarı/aşağı 20 derece sınır
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit highXLimit = new SoftJointLimit { limit = 20f };
                SoftJointLimit lowXLimit = new SoftJointLimit { limit = -20f };
                joint.highAngularXLimit = highXLimit;
                joint.lowAngularXLimit = lowXLimit;
            }
            else
            {
                // RÖMORK AYARI: Dönüşler serbest
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;

                SoftJointLimit angularYLimit = new SoftJointLimit { limit = 60f };
                joint.angularYLimit = angularYLimit;
            }

            // Traktör ve aletin birbirine çarpıp "fiziksel patlama" yapmasını engelle
            IgnoreCollisions(eq.gameObject, true);

            // 4. Sistemi normale döndür
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
                IgnoreCollisions(eq.gameObject, false);
                HarvesterBlade blade = eq.GetComponentInChildren<HarvesterBlade>();
                if (blade != null) blade.isSpinning.Value = false;
            }

            Destroy(currentJoint);
            currentJoint = null;
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