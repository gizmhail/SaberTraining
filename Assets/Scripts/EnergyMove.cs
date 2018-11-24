using ForceBasedMove;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

interface EnergyLockable {
    void EnergyLocked(EnergyMove lockSource);
    void EnergyUnlocked(EnergyMove lockSource);
    bool IsImmunetoEnergyMove(EnergyMove source);
}

public class EnergyMove : MonoBehaviour {
    public Rigidbody lockedRigidbody;
    public SteamVR_Input_Sources handType;
    [Tooltip("Time in seconds before restoring gravity on the locked object")]
    public float delayBeforeRestoringGravity = 1;
    [Tooltip("Button pressure level separating energy raise and energy drag. If 0, any press will lead to a drag, 1 will always raise")]
    public float energyDragStartPressure = 0.2f;

    public List<EnergyParticule> energyParticulePrefabs;

    [Tooltip("Minimal magnitude of hand estimated velocity vector to trigger energy push (trasnfering hand velocity to object when released)")]
    public float minHandVelocityMangitudeForEnergyPush = 1.5f;

    [Header("Object to look search settings")]
    [Tooltip("Size of the search sphere sent in front of the selected hand")]
    public float searchSphereSize = 0.2f;
    [Tooltip("Max raycast search distance")]
    public float maxSearchDistance = 20;

    public bool useAdvanceMovePhysics = true;

    [Header("Advanced move physics settings")]
    public bool doTranslationMove = true;
    public bool doRotationMove = false;
    [Tooltip("The factor used to decrease the movement speed. For instance, if set to 1, a mass of 2 will half the speed.")]
    float massImpactFactor = 0.1f;
    [Tooltip("Base move speed for an object of mass 1")]
    public float baseMoveSpeed = 5.0f;
    [Tooltip("Locked object mass below this level will be considered as this level")]
    public float minLockedMass = 0.5f;
    [Tooltip("Minimum flight time (for an object of mass 1)")]
    public float maxFlightTime = 2.5f;
    [Tooltip("Maximum flight time (for an object of mass 1)")]
    public float minFlightTime = 0.2f;


    private Hand hand;
    private Interactable lockedInteractable;
    private HandSnappable lockedHandSnapable;
    private VelocityEstimator handVelocityEstimator;
    private bool lockedInitialGravity;
    private float initialLockedRaiseDistance = 0;
    private float initialLockedDragDistance = 0;

    // Use this for initialization
    void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void FixedUpdate()
    {
        if (!IsHandConfigured()) return;

        bool energyPressed = SteamVR_Input._default.inActions.InteractUI.GetState(handType);
        float energyLevel = SteamVR_Input._default.inActions.ForceAction.GetAxis(handType);
        if (lockedRigidbody)
        {
            if (!energyPressed)
            {
                UnlockObject(lockedRigidbody);
            }
        }
        else {
            if (energyPressed)
            {
                // We avoid search to energy ock an object, as the user might be currently using the grabbed object
                if (hand.currentAttachedObject == null)
                {
                    SearchObjectToLock();
                }
            }
        }

        if (lockedRigidbody) {
            KeepGravityDisabled();
            if (energyLevel > energyDragStartPressure)
            {
                EnergyDrag();
                UpdateLockedRaiseDistance();
            }
            else {
                EnergyRaise();
                UpdateLockedDragInitialDistance();
            }

        }
        float energyParticuleSpawnProbabilityPercentage = 20;
        float energyParticuleSpawnRadius = 0.05f;
        float minimumDistanceForEnergyParticule = 0.5f;
        if (energyPressed && hand.currentAttachedObject == null && energyParticulePrefabs.Count > 0) {
            var energyParticulePrefab = energyParticulePrefabs[Random.Range(0, energyParticulePrefabs.Count)];
            
            if (energyParticulePrefab != null && Random.Range(0, 100) > energyParticuleSpawnProbabilityPercentage)
            {
                var position = hand.transform.position + energyParticuleSpawnRadius * Random.insideUnitSphere;
                var rotation = hand.transform.rotation;
                if (lockedRigidbody == null)
                {
                    var energyParticule = EnergyParticule.Instantiate(energyParticulePrefab, position, rotation);
                    energyParticule.target = null;
                    energyParticule.targetPosition = HandTargetTransform().position + maxSearchDistance * HandTargetTransform().forward;
                }
                else if(Vector3.Distance(position, lockedRigidbody.transform.position)> minimumDistanceForEnergyParticule)
                {
                    var energyParticule = EnergyParticule.Instantiate(energyParticulePrefab, position, rotation);
                    energyParticule.target = lockedRigidbody.gameObject;
                }
            }
        }
    }

    bool IsHandConfigured() {
        if (hand != null) return true;
        if (Player.instance == null) return false;
        foreach (var availableHand in Player.instance.hands) {
            if (availableHand.handType == handType) {
                hand = availableHand;
                handVelocityEstimator = hand.GetComponent<VelocityEstimator>();
                return true;
            }
        }
        return false;
    }

    void SearchObjectToLock() {
        RaycastHit hit;

        if (Physics.SphereCast(HandTargetTransform().position, searchSphereSize, HandTargetTransform().forward, out hit, maxSearchDistance))
        {
            var targetRb = hit.rigidbody;
            if (targetRb != null)
            {
                LockObject(targetRb);
            }
        }

        Debug.DrawRay(HandTargetTransform().position, HandTargetTransform().forward);
    }

    void LockObject(Rigidbody rb)
    {
        var lockable = rb.GetComponent<EnergyLockable>();
        if (lockable != null && lockable.IsImmunetoEnergyMove(this)) return;
        if (lockedRigidbody != null) {
            UnlockObject(rb);
        }
        lockedRigidbody = rb;
        lockedInteractable = lockedRigidbody.gameObject.GetComponent<Interactable>();
        lockedHandSnapable = lockedRigidbody.gameObject.GetComponent<HandSnappable>();
        lockedInitialGravity = lockedRigidbody.useGravity;
        lockedRigidbody.useGravity = false;
        UpdateLockedRaiseDistance();
        UpdateLockedDragInitialDistance();
        if (lockable != null) {
            lockable.EnergyLocked(this);
        }
    }

    void UnlockObject(Rigidbody rb) {
        var lockable = lockedRigidbody.GetComponent<EnergyLockable>();
        if (lockable != null)
        {
            lockable.EnergyUnlocked(this);
        }
        if (lockedRigidbody == rb) {
            if (handVelocityEstimator != null) {
                Vector3 handVelocity = handVelocityEstimator.GetVelocityEstimate();
                if (handVelocity.magnitude > minHandVelocityMangitudeForEnergyPush) {
                    EnergyPush();
                }
            }
            if (lockedInitialGravity != lockedRigidbody.useGravity) {
                StartCoroutine(DelayGravityRestoration(lockedRigidbody, lockedInitialGravity));
            }
            lockedRigidbody = null;
        }

    }

    void UpdateLockedRaiseDistance()
    {
        initialLockedRaiseDistance = Vector3.Distance(LockedObjectSourceTransform().position, HandTargetTransform().position);
    }

    void UpdateLockedDragInitialDistance()
    {
        initialLockedDragDistance = Vector3.Distance(LockedObjectSourceTransform().position, HandTargetTransform().position);
    }

    Transform LockedObjectSourceTransform() {
        var sourceTransform = lockedRigidbody.transform;
        if (lockedHandSnapable != null) {
            sourceTransform = lockedHandSnapable.GetAttachmentPoint(hand);
        } else if (lockedInteractable != null && lockedInteractable.handFollowTransform != null)
        {
            sourceTransform = lockedInteractable.handFollowTransform;
        }
        return sourceTransform;
    }

    Transform HandTargetTransform()
    {
        return hand.objectAttachmentPoint.transform;
    }

    void KeepGravityDisabled() {
        lockedRigidbody.useGravity = false;
    }

    IEnumerator DelayGravityRestoration(Rigidbody rb, bool targetGravity) {
        yield return new WaitForSeconds(delayBeforeRestoringGravity);
        rb.useGravity = targetGravity;
    }

    void EnergyRaise() {
        if (lockedRigidbody == null) return;
        var targetTransform = HandTargetTransform();
        var raisePosition = targetTransform.position + initialLockedRaiseDistance * targetTransform.forward;
        MoveLockedObjectTowardsTarget(raisePosition, targetTransform.rotation);
    }

    void EnergyDrag()
    {
        if (lockedRigidbody == null) return;
        var targetTransform = HandTargetTransform();
        MoveLockedObjectTowardsTarget(targetTransform);
    }

    void EnergyPush()
    {
        lockedRigidbody.velocity = handVelocityEstimator.GetVelocityEstimate() / MassMoveDecreaseFactor();
    }

    float MassMoveDecreaseFactor() {
        return Mathf.Max(1.0f, massImpactFactor * Mathf.Max(minLockedMass, lockedRigidbody.mass));
    }

    void MoveLockedObjectTowardsTarget(Transform targetTransform)
    {
        MoveLockedObjectTowardsTarget(targetTransform.position, targetTransform.rotation);
    }

    void MoveLockedObjectTowardsTarget(Vector3 position, Quaternion rotation)
    {
        if (lockedRigidbody == null) return;
        var sourceTransform = LockedObjectSourceTransform();

        float moveSpeed = baseMoveSpeed / MassMoveDecreaseFactor();
        if (!useAdvanceMovePhysics)
        {
            //Simple version (in case of inability to use Forcemove sources)
            Vector3 grabDirection = position - sourceTransform.position;
            float currentDistance = grabDirection.magnitude;
            moveSpeed = Mathf.Min(moveSpeed, grabDirection.magnitude);
            lockedRigidbody.velocity = moveSpeed * grabDirection.normalized;
            Quaternion fullRotation = sourceTransform.rotation * Quaternion.Inverse(rotation);
            float rotationSpeed = moveSpeed / 360.0f;
            lockedRigidbody.angularVelocity = rotationSpeed * fullRotation.eulerAngles;
        }
        else {
            float flightTime = Mathf.Min(maxFlightTime, Mathf.Max(minFlightTime, initialLockedDragDistance / moveSpeed));
            // Debug.Log("Flighttime: " + flightTime);
            float damping = 1.2f;
            float frequency = 1.0f/flightTime;
            if(doTranslationMove) lockedRigidbody.AddForceTowards(sourceTransform.position, position, damping, frequency);
            if(doRotationMove) lockedRigidbody.AddTorqueTowards(sourceTransform.rotation, rotation, damping, frequency);
        }
    }
}
