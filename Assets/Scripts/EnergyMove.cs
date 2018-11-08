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
    public float delayBeforeRestoringGravity = 1;
    public float energyDragStartPressure = 0.2f;

    private Hand hand;
    private Interactable lockedInteractable;
    private bool lockedInitialGravity;
    private float initialDistance = 0;


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
                UpdateLockDistance();
            }
            else {
                EnergyRaise();
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
        UpdateLockDistance();
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

    void UpdateLockDistance()
    {
        initialDistance = Vector3.Distance(LockedObjectSourceTransform().position, HandTargetTransform().position);
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
        var raisePosition = targetTransform.position + initialDistance * targetTransform.forward;
        MoveLockedObjectTowardsTarget(raisePosition, targetTransform.rotation);
        Debug.DrawRay(raisePosition, targetTransform.forward);
    }

    void EnergyDrag()
    {
        if (lockedRigidbody == null) return;
        var targetTransform = HandTargetTransform();
        MoveLockedObjectTowardsTarget(targetTransform);
    }

    void MoveLockedObjectTowardsTarget(Transform targetTransform)
    {
        MoveLockedObjectTowardsTarget(transform.position, transform.rotation);
    }

    void MoveLockedObjectTowardsTarget(Vector3 position, Quaternion rotation)
    {
        if (lockedRigidbody == null) return;
        var sourceTransform = LockedObjectSourceTransform();

        //Simple version (in case of inability to use Forcemove sources)
        //Vector3 grabDirection = position - sourceTransform.position;
        //lockedRigidbody.AddForce(forceCatchFactor * grabDirection);

        float damping = 1.2f;
        float frequency = 0.4f;
        lockedRigidbody.AddForceTowards(sourceTransform.position, position, damping, frequency);
        lockedRigidbody.AddTorqueTowards(sourceTransform.rotation, rotation, damping, frequency);
        Debug.DrawRay(sourceTransform.position, position - sourceTransform.position);
    }
}
