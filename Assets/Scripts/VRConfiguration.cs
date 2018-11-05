using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class VRConfiguration : MonoBehaviour
{

    private void Awake()
    {
        // Register to be notified when hand models have been set up 
        var renderModelLoadedAction = SteamVR_Events.RenderModelLoadedAction(HideControllers);
        renderModelLoadedAction.enabled = true;
    }

    private void HideControllers(SteamVR_RenderModel renderModelLoaded, bool action)
    {
        for (int handIndex = 0; handIndex < Player.instance.hands.Length; handIndex++)
        {
            Hand hand = Player.instance.hands[handIndex];
            if (hand != null)
            {
                hand.HideController(true);
                hand.SetSkeletonRangeOfMotion(Valve.VR.EVRSkeletalMotionRange.WithoutController);
            }
        }
    }
}
