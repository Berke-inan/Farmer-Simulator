using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(TractorController))] // Bu script Traktörde olmak zorundadýr
public class TractorEquipmentManager : NetworkBehaviour
{
    [Header("Römork / Ekipman Baðlantýsý")]
    public Transform tractorHitchPoint;

    private AttachableEquipment equipmentInRange;
    private ConfigurableJoint currentJoint;
    private TractorController tractorController;

    private void Awake()
    {
        tractorController = GetComponent<TractorController>();
    }

    private void Update()
    {
        // Sadece aracý ben sürüyorsam 'F' tuþunu dinle
        if (tractorController.IsDrivenByMe)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (currentJoint == null && equipmentInRange != null)
                {
                    AttachEquipmentServerRpc(equipmentInRange.NetworkObjectId);
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
        // Patron Traktör (Zamk Kýrýcý) römorkun tekerleklerini yönetir
        if (!tractorController.IsDrivenByMe) return;

        if (currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null)
            {
                eq.GetComponent<Rigidbody>().WakeUp();

                // Traktör gaza basýyor mu bilgisini diðer scriptten alýyoruz
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
        if (eq != null && equipmentInRange == eq) equipmentInRange = null;
    }

    [Rpc(SendTo.Server)]
    private void AttachEquipmentServerRpc(ulong equipmentNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(equipmentNetworkId, out NetworkObject netObj))
        {
            AttachableEquipment eq = netObj.GetComponent<AttachableEquipment>();

            eq.transform.position = tractorHitchPoint.position - (eq.hitchPoint.position - eq.transform.position);

            ConfigurableJoint joint = gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = eq.GetComponent<Rigidbody>();
            joint.anchor = tractorHitchPoint.localPosition;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;

            IgnoreCollisions(eq.gameObject, true);

            currentJoint = joint;
            eq.GetComponent<Rigidbody>().WakeUp();
        }
    }

    [Rpc(SendTo.Server)]
    private void DetachEquipmentServerRpc()
    {
        if (currentJoint != null)
        {
            AttachableEquipment eq = currentJoint.connectedBody.GetComponent<AttachableEquipment>();
            if (eq != null) IgnoreCollisions(eq.gameObject, false);

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