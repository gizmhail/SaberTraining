using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using ForceBasedMove;

[RequireComponent(typeof(VelocityEstimator), typeof(Rigidbody), typeof(Interactable))]
public class ForceSaber : MonoBehaviour, EnergyLockable {
    public float forceThrowMinimalVelocity = 2;

    public AudioClip saberActive;
    public AudioClip saberToggleOn;
    public AudioClip saberToggleOff;
    public float delayFromOnToActiveClip = 0.5f;
    public List<GameObject> energySpots;

    public bool isSaberActive = false;
    Hand attachedHand = null;
    Rigidbody rb;
    VelocityEstimator velocityEstimator;
    Interactable interactable;
    Animator animator;
    AudioSource audioSource;

    float gravityPausedTime = 0;
    float gravityPauseDuration = 0;

    bool energyLocked = false;

    private bool grabPressed = false;
    private float grabPauseTime = 0;
    private bool callingBackSaber = false;

    #region Monobehavior 
    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
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
        if (SteamVR_Input._default.inActions.GrabGrip.GetState(SteamVR_Input_Sources.Any))
        {
            if (grabPauseTime != 0 && (Time.time - grabPauseTime) < 0.5f)
            {
                callingBackSaber = true;
            }
            else {
                grabPauseTime = 0;
            }
            grabPressed = true;
        }
        else
        {
            if (grabPressed == true) {
                grabPauseTime = Time.time;
            }
            grabPressed = false;
            callingBackSaber = false;
        }
        if (callingBackSaber)
        {
            if (isSaberActive) ToggleSaber();
            PauseGravity(duration: 2);
            Transform sourceTransform = transform;
            if (interactable != null && interactable.handFollowTransform != null)
            {
                sourceTransform = interactable.handFollowTransform;
            }
            Hand hand = Player.instance.leftHand;
            if (!SteamVR_Input._default.inActions.GrabGrip.GetState(SteamVR_Input_Sources.LeftHand)) {
                hand = Player.instance.rightHand;
            }
            rb.AddForceTowards(sourceTransform.position, hand.objectAttachmentPoint.transform.position);
        }
        if (!energyLocked) {
            rb.useGravity = (gravityPausedTime == 0);
        }
    }
    #endregion

    void PlaySaberActiveClip()
    {
        if (isSaberActive)
        {
            //audioSource.Stop();
            audioSource.clip = saberActive;
            if(!audioSource.isPlaying) audioSource.Play();
            audioSource.loop = true;
        }
    }
    void ToggleSaber()
    {
        isSaberActive = !animator.GetBool("toggleOn");
        animator.SetBool("toggleOn", isSaberActive);
        ///audioSource.Stop();
        if (isSaberActive) {
            audioSource.clip = saberToggleOn;
            Invoke("PlaySaberActiveClip", delayFromOnToActiveClip);
        }
        else{
            audioSource.clip = saberToggleOff;
        }
        audioSource.loop = false;
        audioSource.Play();
    }

    #region Interactable callbacks
    void OnDetachedFromHand(Hand hand)
    {
        //Debug.Log("Throw velocity: " + rb.velocity);
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
        callingBackSaber = false;
        grabPauseTime = 0;
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
        foreach (var energySpot in energySpots) energySpot.SetActive(true);
    }

    void ReEnableGravity()
    {
        gravityPausedTime = 0;
        foreach (var energySpot in energySpots) energySpot.SetActive(false);
    }
    #endregion

    #region EnergyLockable
    void EnergyLockable.EnergyLocked(EnergyMove lockSource)
    {
        energyLocked = true;
    }

    void EnergyLockable.EnergyUnlocked(EnergyMove lockSource)
    {
        energyLocked = false;
        // We prevent our manipulation of the gravity to meddle with gravity restoration by the lock source
        PauseGravity(duration: lockSource.delayBeforeRestoringGravity);
    }

    bool EnergyLockable.IsImmunetoEnergyMove(EnergyMove source)
    {
        return false;
    }

    #endregion
}
