using ForceMove;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(VelocityEstimator), typeof(Rigidbody), typeof(Interactable))]
public class ForceSaber : MonoBehaviour {
    public float forceCatchFactor = 3;
    public Transform forceGrabTarget;
    public float forceThrowMinimalVelocity = 2;

    Hand attachedHand = null;
    Rigidbody rb;
    VelocityEstimator velocityEstimator;
    Interactable interactable;
    Animator animator;

    float gravityPausedTime = 0;
    float gravityPauseDuration = 0;

    #region Monobehavior
    private void Awake()
    {
        if (forceGrabTarget == null) forceGrabTarget = transform;
    }

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<Interactable>();
        velocityEstimator = GetComponent<VelocityEstimator>();
        interactable.onDetachedFromHand += OnDetachedFromHand;
        interactable.onAttachedToHand += OnAttachedToHand;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if ((Time.time - gravityPausedTime) > gravityPauseDuration) {
            ReEnableGravity();
        }

        if (attachedHand != null &&SteamVR_Input._default.inActions.InteractUI.GetStateDown(attachedHand.handType))
        {
            ToggleSaber();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (SteamVR_Input._default.inActions.InteractUI.GetState(SteamVR_Input_Sources.LeftHand))
        {
            // ForceCatch();
        }
        rb.useGravity = (gravityPausedTime == 0);

        if (SteamVR_Input._default.inActions.InteractUI.GetState(SteamVR_Input_Sources.LeftHand))
        {
            RaycastHit hit;
            //if (Physics.Raycast(Player.instance.leftHand.objectAttachmentPoint.transform.position, Player.instance.leftHand.objectAttachmentPoint.transform.forward, out hit, 20))
            if (Physics.SphereCast(Player.instance.leftHand.objectAttachmentPoint.transform.position, 0.2f, Player.instance.leftHand.objectAttachmentPoint.transform.forward, out hit, 20))
            {
                //Debug.Log("Aiming at: " + hit.transform.name);
                var targetRb = hit.rigidbody;
                if (targetRb != null) {
                    var targetTransform = hit.transform;
                    var targetInteractable = hit.transform.gameObject.GetComponent<Interactable>();
                    if (targetInteractable != null && targetInteractable.handFollowTransform != null) {
                        targetTransform = targetInteractable.handFollowTransform;
                    }
                    targetRb.AddForceTowards(targetTransform.position, Player.instance.leftHand.objectAttachmentPoint.transform.position, 1.2f, 0.4f);
                    targetRb.AddTorqueTowards(targetTransform.rotation, Player.instance.leftHand.objectAttachmentPoint.transform.rotation, 1.2f, 0.4f);
                    targetRb.useGravity = false;
                    //TODO Reenable gravity
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
    }
    #endregion

    void ToggleSaber()
    {
        animator.SetBool("toggleOn", !animator.GetBool("toggleOn"));
    }

    #region Interactable callbacks
    void OnDetachedFromHand(Hand hand)
    {
        Debug.Log("Throw velocity: " + rb.velocity);
        if (rb.velocity.magnitude > forceThrowMinimalVelocity)
        {
            PauseGravity(duration: 5);
        }
        else {
            ReEnableGravity();
        }
        attachedHand = null;
    }

    void OnAttachedToHand(Hand hand)
    {
        ReEnableGravity();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        attachedHand = hand;
    }
    #endregion

    #region Gravity manipulation
    /// <summary>
    /// Disable gravity for a few seconds
    /// </summary>
    /// <param name="duration">Duration of gravity desactivation</param>
    void PauseGravity(float duration) {
        gravityPausedTime = Time.time;
        gravityPauseDuration = duration;
    }

    void ReEnableGravity()
    {
        gravityPausedTime = 0;
    }
    #endregion

    /// <summary>
    /// Move (based on physics) this saber to left hand
    /// </summary>
    void ForceCatch()
    {
        PauseGravity(duration: 1);

        //Simple version (in case of inability to use Forcemove sources)
        //Vector3 grabDirection = Player.instance.leftHand.objectAttachmentPoint.transform.position - forceGrabTarget.position;
        //rb.AddForce(forceCatchFactor * grabDirection);
        rb.AddForceTowards(initialPosition: forceGrabTarget.position, destinationPosition: Player.instance.leftHand.objectAttachmentPoint.transform.position);
        rb.AddTorqueTowards(initialRotation: forceGrabTarget.rotation, destinationRotation: Player.instance.leftHand.objectAttachmentPoint.transform.rotation);
    }
}
