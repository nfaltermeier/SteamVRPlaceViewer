using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

public class VRControlScript : MonoBehaviour
{   
    private MainViewerScript.DisplayMode[] modes;

    public MainViewerScript.DisplayMode CurrentDisplayMode;
    private int _modeIndex;

    public MainViewerScript MainScript;
    public LegendVisualScript LegendScript;
    public TimelineVisualScript TimelineScript;
    public HighlighterScript HighlighterScript;

    public Animator LeftMovementAnimator;
    public Animator RightMovementAnimator;

    public Transform LeftHand;
    public Transform RightHand;
    public Transform Control;
    public Transform HandAverage;
    private MotionDampenerScript _dampener;

    public bool MoveMode;
    public bool ScaleMode;
    
    private bool _scaling;
    private float _initialScale;
    private float _initialHandDistance;

    private bool _lastLeftHand;
    private bool _lastRightHand;

    private bool _lastCycleUp;
    private bool _lastCycleDown;

    void Start ()
    {
        _dampener = Control.GetComponent<MotionDampenerScript>();
        modes = new MainViewerScript.DisplayMode[] { MainViewerScript.DisplayMode.ColorAndHeat, MainViewerScript.DisplayMode.FullHeat, MainViewerScript.DisplayMode.FullLongevity, MainViewerScript.DisplayMode.FlatColor };
        ApplyDisplayMode();
    }

    void Update ()
    {
        //HandAverage is the average point between both hands that controls the board when both grip buttons are pressed
        HandAverage.position = Vector3.Lerp(LeftHand.position, RightHand.position, .5f);
        HandAverage.rotation = Quaternion.Lerp(LeftHand.rotation, RightHand.rotation, .5f);
        HandAverage.LookAt(LeftHand.position, HandAverage.up);

        
        bool leftHand = SteamVR_Input.GetState("GrabGrip", SteamVR_Input_Sources.LeftHand);
        bool rightHand = SteamVR_Input.GetState("GrabGrip", SteamVR_Input_Sources.RightHand);

        // Not really sure what this did, but I don't think SteamVR has this built in
        // SetMovementIndicators(leftHand, rightHand);

        _dampener.Target = GetDampenerTarget(leftHand, rightHand);

        ScaleMode = leftHand && rightHand;

        UpdateDisplayMode();

        UpdateScaleMode();
        UpdateTimeline();

        HighlighterScript.ShowHighlight = SteamVR_Input.GetState("InteractUI", SteamVR_Input_Sources.RightHand);

    }

    private void SetMovementIndicators(bool leftHand, bool rightHand)
    {
        bool newBothHands = leftHand && rightHand && !(_lastLeftHand && _lastRightHand);
        if (leftHand && !_lastLeftHand || newBothHands)
        {
            LeftMovementAnimator.SetTrigger("Bump");
        }
        if (rightHand && !_lastRightHand || newBothHands)
        {
            RightMovementAnimator.SetTrigger("Bump");
        }
        _lastLeftHand = leftHand;
        _lastRightHand = rightHand;
    }

    private Transform GetDampenerTarget(bool leftHand, bool rightHand)
    {
        if(leftHand && rightHand)
        {
            return HandAverage;
        }
        if(leftHand)
        {
            return LeftHand;
        }
        if(rightHand)
        {
            return RightHand;
        }
        return null;
    }

    private void UpdateDisplayMode()
    {
        bool cycleUp = SteamVR_Input.GetState("CycleUp", SteamVR_Input_Sources.RightHand) || SteamVR_Input.GetState("CycleUp", SteamVR_Input_Sources.LeftHand);
        bool cycleDown = SteamVR_Input.GetState("CycleDown", SteamVR_Input_Sources.RightHand) || SteamVR_Input.GetState("CycleDown", SteamVR_Input_Sources.LeftHand);
        if (cycleUp && !_lastCycleUp)
        {
            _modeIndex = (_modeIndex + 1) % modes.Length;
            ApplyDisplayMode();
        }
        if (cycleDown && !_lastCycleDown)
        {
            _modeIndex = _modeIndex - 1;
            if (_modeIndex == -1)
            {
                _modeIndex = modes.Length - 1;
            }
            ApplyDisplayMode();
        }
        _lastCycleUp = cycleUp;
        _lastCycleDown = cycleDown;
    }

    private void UpdateScaleMode()
    {
        if (ScaleMode)
        {
            float dist = (LeftHand.position - RightHand.position).magnitude;
            if (!_scaling)
            {
                _scaling = true;
                _initialScale = Control.transform.localScale.x;
                _initialHandDistance = dist;
            }
            else
            {
                float newScale = _initialScale * (dist / _initialHandDistance);
                Control.transform.localScale = new Vector3(newScale, newScale, newScale);
            }
        }
        else
        {
            _scaling = false;
        }
    }

    private void ApplyDisplayMode()
    {
        MainScript.SetDisplayMode(modes[_modeIndex]);
        LegendScript.SetDisplayMode(modes[_modeIndex]);
    }

    private void UpdateTimeline()
    {
        bool timelineTrigger = SteamVR_Input.GetState("InteractUI", SteamVR_Input_Sources.LeftHand);
        TimelineScript.ActiveTimeline = timelineTrigger;

        float leftThumbstick = SteamVR_Input.GetVector2("Timeline", SteamVR_Input_Sources.LeftHand).x / 1000;
        float rightThumbstick = SteamVR_Input.GetVector2("Timeline", SteamVR_Input_Sources.RightHand).x / 1000;
        MainScript.Time = Mathf.Clamp01(leftThumbstick + rightThumbstick + MainScript.Time);
    }
}
