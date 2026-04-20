using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(TractorController))]
public class TractorEquipmentManager : NetworkBehaviour
{
    [Header("Bağlantı Noktaları (Hitch Points)")]
    public Transform rearHitchPoint;  // Römork için (Arka)
    public Transform frontHitchPoint; // Biçer için (Ön)

    private AttachableEquipment equipmentInRange;
    private string currentTriggerSide = ""; // "Front" veya "Rear"
    private ConfigurableJoint currentJoint;
    private TractorController tractorController;

    private void Awake()
    {
        tractorController = GetComponent<TractorController>();
    }

    private void Update()
    {
        // Sadece aracı ben sürüyorsam 'F' tuşunu dinle
        if (tractorController.IsDrivenByMe)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (currentJoint == null && equipmentInRange != null)
                {
                    // --- YENİ VE KUSURSUZ MANTIK: Mesafe (Distance) Kontrolü ---
                    // Ekipman traktörün ön topuzuna mı daha yakın, arka topuzuna mı?
                    float distToFront = Vector3.Distance(equipmentInRange.hitchPoint.position, frontHitchPoint.position);
                    float distToRear = Vector3.Distance(equipmentInRange.hitchPoint.position, rearHitchPoint.position);

                    string detectedSide = (distToFront < distToRear) ? "Front" : "Rear";

                    bool isCorrectSide = false;
                    if (equipmentInRange.type == AttachableEquipment.EquipmentType.Trailer && detectedSide == "Rear") isCorrectSide = true;
                    if (equipmentInRange.type == AttachableEquipment.EquipmentType.Header && detectedSide == "Front") isCorrectSide = true;

                    if (isCorrectSide)
                    {
                        AttachEquipmentServerRpc(equipmentInRange.NetworkObjectId, detectedSide);
                    }
                    else
                    {
                        Debug.LogWarning("Bu ekipman, traktörün bu tarafına takılamaz!");
                    }
                }
                else if (currentJoint != null)
                {
                    DetachEquipmentServerRpc();
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // ========================================================
        // 1. SUNUCU ZEKASI: Bıçak Güvenliği (Yetki kaybından etkilenmez)
        // ========================================================
        if (IsServer && currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null)
            {
                HarvesterBlade blade = eq.GetComponentInChildren<HarvesterBlade>();
                if (blade != null)
                {
                    // Traktörün dolu olup olmadığını kontrol et ve bıçağa bildir
                    if (blade.isSpinning.Value != tractorController.IsOccupied)
                    {
                        blade.isSpinning.Value = tractorController.IsOccupied;
                    }
                }
            }
        }

        // ========================================================
        // 2. SÜRÜCÜ ZEKASI: Tekerlek Zamkı Kırıcı (Sadece şoför çalıştırır)
        // ========================================================
        if (!tractorController.IsDrivenByMe) return; // Şoför değilse buradan aşağısı çalışmaz!

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
        // Artık isimlere bakmıyoruz, ekipmanın traktörün yanına gelmesi yeterli
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

    // RPC'ye 'side' parametresi eklendi ki sunucu hangi topuza bağlanacağını bilsin
    [Rpc(SendTo.Server)]
    private void AttachEquipmentServerRpc(ulong equipmentNetworkId, string side)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(equipmentNetworkId, out NetworkObject netObj))
        {
            AttachableEquipment eq = netObj.GetComponent<AttachableEquipment>();
            Rigidbody eqRb = eq.GetComponent<Rigidbody>();
            Transform targetPoint = (side == "Front") ? frontHitchPoint : rearHitchPoint;

            // 1. Çarpışmaları kapat ve hızı sıfırla (Fırlamayı önler)
            eqRb.isKinematic = true;
            eqRb.detectCollisions = false;

            // ==========================================
            // 2. SADECE POZİSYON (DÖNDÜRME İPTAL EDİLDİ)
            // ==========================================
            // Obje o an hangi açıdaysa o açıda kalır, sadece ucu ucuna çekilir.
            eq.transform.position = targetPoint.position - (eq.hitchPoint.position - eq.transform.position);

            Physics.SyncTransforms();

            // 3. BAĞLANTI (JOINT)
            ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = eqRb;
            joint.anchor = transform.InverseTransformPoint(targetPoint.position);

            // İleri-geri, sağa-sola kopmaları engelle
            joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;

            if (eq.type == AttachableEquipment.EquipmentType.Header)
            {
                // BİÇERDÖVER AYARI: Sağa sola dönüş ve yan yatma TAMAMEN KİLİTLİ
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;

                // Yukarı/Aşağı (Pitch) hareketine senin istediğin gibi 20 DERECE SINIR
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                SoftJointLimit highXLimit = new SoftJointLimit();
                highXLimit.limit = 20f; // Yukarı 20 derece
                SoftJointLimit lowXLimit = new SoftJointLimit();
                lowXLimit.limit = -20f; // Aşağı 20 derece

                joint.highAngularXLimit = highXLimit;
                joint.lowAngularXLimit = lowXLimit;
            }
            else
            {
                // RÖMORK AYARI (Eski çalışan hali)
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;

                SoftJointLimit angularYLimit = new SoftJointLimit();
                angularYLimit.limit = 60f;
                joint.angularYLimit = angularYLimit;
            }

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

                // Ekipman çözülünce bıçak güvenlik için anında dursun
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
}