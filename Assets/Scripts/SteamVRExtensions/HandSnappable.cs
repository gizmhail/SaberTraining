using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Throwable))]
public class HandSnappable : MonoBehaviour {
    public Transform leftAttachmentOffset;
    public Transform rightAttachmentOffset;

    Throwable throwable;
    Interactable interactable;

    #region MonoBehavior
    void Start () {
        throwable = GetComponent<Throwable>();
        throwable.attachmentFlags |= Hand.AttachmentFlags.SnapOnAttach;
        interactable = GetComponent<Interactable>();
        interactable.onAttachedToHand += OnAttachedToHand;
        if (leftAttachmentOffset == null)
        {
            leftAttachmentOffset = rightAttachmentOffset;
        }
        if (rightAttachmentOffset == null)
        {
            rightAttachmentOffset = leftAttachmentOffset;
        }
        if (leftAttachmentOffset == null && rightAttachmentOffset == null) {
            rightAttachmentOffset = transform;
            leftAttachmentOffset = transform;
        }
    }
    #endregion

    #region Interactable Callback
    private void OnAttachedToHand(Hand hand)
    {
        ActivateSnapPoint(hand);
    }
    #endregion
    
    protected virtual void ActivateSnapPoint(Hand hand) {
        Transform attachmentOffset = GetAttachmentPoint(hand);
        if (interactable.handFollowTransform != attachmentOffset)
        {
            interactable.handFollowTransform = attachmentOffset;
            throwable.attachmentOffset = attachmentOffset;
            hand.AttachObject(gameObject, GrabTypes.Grip, throwable.attachmentFlags, attachmentOffset);
        }
    }

    public Transform GetAttachmentPoint(Hand hand) {
        Transform attachmentOffset = rightAttachmentOffset;
        if (hand.handType == SteamVR_Input_Sources.LeftHand)
        {
            attachmentOffset = leftAttachmentOffset;
        }
        return attachmentOffset;
    }
}
