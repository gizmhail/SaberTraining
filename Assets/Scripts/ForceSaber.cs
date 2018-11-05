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
    public GameObject blade;
    Hand attachedHand = null;

    Rigidbody rb;
    Rigidbody bladeRb;
    FixedJoint bladeFixedJoint;
    VelocityEstimator velocityEstimator;
    Interactable interactable;
    Animator animator;

    float gravityPausedTime = 0;
    float gravityPauseDuration = 0;

    private void Awake()
    {
        if (forceGrabTarget == null) forceGrabTarget = transform;
    }

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
        bladeRb = blade.GetComponent<Rigidbody>();
        interactable = GetComponent<Interactable>();
        velocityEstimator = GetComponent<VelocityEstimator>();
        interactable.onDetachedFromHand += OnDetachedFromHand;
        interactable.onAttachedToHand += OnAttachedToHand;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if ((Time.time - gravityPausedTime) > gravityPauseDuration) {
            gravityPausedTime = 0;
        }

        if (attachedHand != null &&SteamVR_Input._default.inActions.InteractUI.GetStateDown(attachedHand.handType))
        {
            animator.SetBool("toggleOn", !animator.GetBool("toggleOn"));
        }
    }

    void ReenableGravity()
    {
        gravityPausedTime = 0;

    }
    void OnDetachedFromHand(Hand hand)
    {
        Debug.Log("Throw velocity: " + rb.velocity);
        if (rb.velocity.magnitude > 2)
        {
            gravityPausedTime = Time.time;
            gravityPauseDuration = 5;
        }
        else {
            gravityPausedTime = 0;
        }
        attachedHand = null;
    }

    void OnAttachedToHand(Hand hand)
    {
        gravityPausedTime = 0;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        attachedHand = hand;
    }

    // Update is called once per frame
    void FixedUpdate () {
        if (SteamVR_Input._default.inActions.InteractUI.GetState(SteamVR_Input_Sources.Any))
        {

            gravityPausedTime = Time.time;
            gravityPauseDuration = 1;

            //Simple version (in csae of inability to use Forcemove sources)
            //Vector3 grabDirection = Player.instance.leftHand.objectAttachmentPoint.transform.position - forceGrabTarget.position;
            //rb.AddForce(forceCatchFactor * grabDirection);
            rb.AddForceTowards(initialPosition: forceGrabTarget.position, destinationPosition: Player.instance.leftHand.objectAttachmentPoint.transform.position);
            rb.AddTorqueTowards(initialRotation: forceGrabTarget.rotation, destinationRotation: Player.instance.leftHand.objectAttachmentPoint.transform.rotation);

        }

        rb.useGravity = (gravityPausedTime == 0);

    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("*** COLLISION ***");
        Debug.Log("Relative velocity: "+collision.relativeVelocity);
        Debug.Log("Saber velocity: " + rb.velocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        Vector3 velocity = velocityEstimator.GetVelocityEstimate();
        var otherRb = other.GetComponent<Rigidbody>();
        if (otherRb)
        {
            var massPonderation = rb.mass / otherRb.mass;
            var otherImpact = massPonderation * velocity;
            var selfImpact = (otherRb.mass / rb.mass) * otherRb.velocity;
            otherRb.velocity += otherImpact;
            rb.velocity += selfImpact;

        }
    }
}
