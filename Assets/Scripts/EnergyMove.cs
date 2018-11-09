using ForceMove;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

interface EnergyLockable {
    void EnergyLocked(EnergyMove lockSource);
    void EnergyUnlocked(EnergyMove lockSource);
}

public class EnergyMove : MonoBehaviour {
    public Rigidbody lockedRigidbody;
    public SteamVR_Input_Sources handType;
    [Tooltip("Time in seconds before restoring gravity on the locked object")]
    public float delayBeforeRestoringGravity = 1;
    [Tooltip("Button pressure level separating energy raise and energy drag. If 0, any press will lead to a drag, 1 will always raise")]
    public float energyDragStartPressure = 0.2f;

    [Header("Object to look search settings")]
    [Tooltip("Size of the search sphere sent in front of the selected hand")]
    public float searchSphereSize = 0.2f;
    [Tooltip("Max raycast search distance")]
    public float maxSearchDistance = 20;

    public bool useAdvanceMovePhysics = true;
    
    [Header("Advanced move physics settings")]
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
                SearchObjectToLock();
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
    }

    bool IsHandConfigured() {
        if (hand != null) return true;
        if (Player.instance == null) return false;
        foreach (var availableHand in Player.instance.hands) {
            if (availableHand.handType == handType) {
                hand = availableHand;
                return true;
            }
        }
        return false;
    }

    void SearchObjectToLock() {
        RaycastHit hit;
        float searchSphereSize = 0.2f;
        float maxSearchDistance = 20;
        if (Physics.SphereCast(hand.objectAttachmentPoint.transform.position, searchSphereSize, hand.objectAttachmentPoint.transform.forward, out hit, maxSearchDistance))
        {
            var targetRb = hit.rigidbody;
            if (targetRb != null)
            {
                LockObject(targetRb);
            }
        }
    }

    void LockObject(Rigidbody rb)
    {
        if (lockedRigidbody != null) {
            UnlockObject(rb);
        }
        lockedRigidbody = rb;
        lockedInteractable = lockedRigidbody.gameObject.GetComponent<Interactable>();
        lockedInitialGravity = lockedRigidbody.useGravity;
        lockedRigidbody.useGravity = false;
        UpdateLockedRaiseDistance();
        UpdateLockedDragInitialDistance();
        var lockable = lockedRigidbody.GetComponent<EnergyLockable>();
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
        if (lockedInteractable != null && lockedInteractable.handFollowTransform != null)
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

    void MoveLockedObjectTowardsTarget(Transform targetTransform)
    {
        MoveLockedObjectTowardsTarget(targetTransform.position, targetTransform.rotation);
    }

    void MoveLockedObjectTowardsTarget(Vector3 position, Quaternion rotation)
    {
        if (lockedRigidbody == null) return;
        var sourceTransform = LockedObjectSourceTransform();

        float moveSpeed = baseMoveSpeed / Mathf.Max(minLockedMass, lockedRigidbody.mass);
        if (!useAdvanceMovePhysics)
        {
            //Simple version (in case of inability to use Forcemove sources)
            Vector3 grabDirection = position - sourceTransform.position;
            float currentDistance = grabDirection.magnitude;
            moveSpeed = Mathf.Min(moveSpeed, grabDirection.magnitude);
            lockedRigidbody.velocity = moveSpeed * grabDirection.normalized;
        }
        else {
            float flightTime = Mathf.Min(maxFlightTime, Mathf.Max(minFlightTime, initialLockedDragDistance / moveSpeed));
            Debug.Log("Flighttime: " + flightTime);
            float damping = 1.2f;
            float frequency = 1.0f/flightTime;
            lockedRigidbody.AddForceTowards(sourceTransform.position, position, damping, frequency);
            lockedRigidbody.AddTorqueTowards(sourceTransform.rotation, rotation, damping, frequency);
        }
    }
}
