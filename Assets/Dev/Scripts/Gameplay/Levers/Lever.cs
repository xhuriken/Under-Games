using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Possible states for a lever. (ON or OFF)
/// </summary>
public enum LeverState
{
    Off = 0,
    On = 1
}

/// <summary>
/// Who owns this lever ? (player or opponent).
/// </summary>
public enum LeverOwner
{
    Player,
    Opponent
}

/// <summary>
/// EVENT FOR THE GAME MANAGER
/// UnityEvent raised when a lever state changes. ! 
/// </summary>
[Serializable]
public class LeverStateChangedEvent : UnityEvent<Lever, LeverState> { }

/// <summary>
/// Controls for a single lever: state, visuals (his handle and LED) and mouse click interaction. <br></br>
/// TODO: Animations for led and handle movement !
/// </summary>
public class Lever : MonoBehaviour
{

    //--------------------------------
    [Header("Setup")]
    [SerializeField] private LeverOwner owner = LeverOwner.Player;

    [Tooltip("Transform of the lever bar/handle that will rotate when toggled.")]
    [SerializeField] private Transform handleTransform;

    [Tooltip("Local rotation when the lever is OFF.")]
    [SerializeField] private Vector3 offLocalEulerAngles = new Vector3(-30f, 0f, 0f);

    [Tooltip("Local rotation when the lever is ON.")]
    [SerializeField] private Vector3 onLocalEulerAngles = new Vector3(30f, 0f, 0f);

    [Header("LED")]
    [Tooltip("Renderer of the LED object (child of LED SUPPORT).")]
    [SerializeField] private Renderer ledRenderer;

    [Tooltip("LED color when lever is ON (base color, without HDR intensity).")]
    [SerializeField] private Color ledOnColor = Color.green;

    [Tooltip("LED color when lever is OFF.")]
    [SerializeField] private Color ledOffColor = Color.red;

    [Tooltip("HDR intensity multiplier when LED is ON.")]
    [Min(0f)]
    [SerializeField] private float ledOnIntensity = 4f;

    [Tooltip("HDR intensity multiplier when LED is OFF.")]
    [Min(0f)]
    [SerializeField] private float ledOffIntensity = 4f;
    
    //
    //TODO: The latest ON state changed, need to make different led animation (Green + red flashing?)!
    //

    //--------------------------------
    [Header("Events")]
    [Tooltip("Invoked when the lever state changes.")]
    [SerializeField] private LeverStateChangedEvent onStateChanged;

    public LeverStateChangedEvent OnStateChanged => onStateChanged;
    private LeverState _state = LeverState.Off;
    private MaterialPropertyBlock _mpb;

    /// <summary>
    /// Current state of the lever (read-only from outside).
    /// Use SetState() or Toggle() method to change it.
    /// </summary>
    public LeverState State => _state;

    /// <summary>
    /// Owner of this lever (player or opponent).
    /// </summary>
    public LeverOwner Owner => owner;

    private void Awake()
    {
        // All levers start OFF
        _state = LeverState.Off;

        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        UpdateVisuals();
    }

    /// <summary>
    /// Called when the user clicks on this object. (Unity native)
    /// Requires a Collider component !
    /// </summary>
    private void OnMouseDown()
    {
        // It's juste point and click
        Toggle();
    }

    /// <summary>
    /// Toggle the lever state between ON and OFF.
    /// </summary>
    public void Toggle()
    {
        //Its: State == if on, set it to off else on
        SetState(_state == LeverState.On ? LeverState.Off : LeverState.On);
    }

    /// <summary>
    /// Sets the lever state and updates visuals + events.
    /// </summary>
    /// <param name="newState">New state to apply.</param>
    /// <param name="invokeEvent">If true: invokes the OnStateChanged event.</param>
    public void SetState(LeverState newState, bool invokeEvent = true)
    {
        if (_state == newState)
            return;

        _state = newState;
        UpdateVisuals();

        // I set it because we never know if we'll use it, but we can remove it later btw
        if (invokeEvent)
        {
            onStateChanged?.Invoke(this, _state);
        }
    }

    /// <summary>
    /// Updates handle rotation and LED emission color based on the current state.
    /// </summary>
    private void UpdateVisuals()
    {
        UpdateHandleRotation();
        UpdateLedEmission();
    }

    /// <summary>
    /// Rotates the handle transform to match the current state.
    /// </summary>
    private void UpdateHandleRotation()
    {
        if (handleTransform == null)
            return;

        // Determine target Euler angles based on the current state
        Vector3 targetEuler = 
            _state == LeverState.On? onLocalEulerAngles : offLocalEulerAngles;

        // Set the local rotation to the target Euler angles
        handleTransform.localRotation = Quaternion.Euler(targetEuler);
    }

    /// <summary>
    /// Updates the LED emission color using a MaterialPropertyBlock.
    /// HDR is handled by multiplying the base color by a gamma-corrected intensity.
    /// TODO: ANIMATE THE LED COLOR CHANGE !!! (And idle ?)
    /// </summary>
    private void UpdateLedEmission()
    {
        if (ledRenderer == null)
            return;

        // Get the current MaterialPropertyBlock from the renderer
        ledRenderer.GetPropertyBlock(_mpb);

        //Determine color and intensity of the LED based on the current state
        Color baseColor = _state == LeverState.On ? ledOnColor : ledOffColor;
        float intensity = _state == LeverState.On ? ledOnIntensity : ledOffIntensity;

        // Convert linear intensity to gamma space for HDR emission.
        Color emissionColor = baseColor * Mathf.LinearToGammaSpace(intensity);

        // Set the emission color in the MaterialPropertyBlock
        _mpb.SetColor("_EmissionColor", emissionColor);
        // Apply the updated MaterialPropertyBlock to the LED renderer !
        ledRenderer.SetPropertyBlock(_mpb);
    }
}
