using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// Possible states for one lever. (ON or OFF)
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
    [SerializeField] private Vector3 offLocalEulerAngles = new Vector3(-130f, 0f, 0f);

    [Tooltip("Local rotation when the lever is ON.")]
    [SerializeField] private Vector3 onLocalEulerAngles = new Vector3(-60f, 0f, 0f);

    [Header("LED")]
    [Tooltip("Renderer of the LED object (child of LED SUPPORT).")]
    [SerializeField] private Renderer ledRenderer;

    [Tooltip("LED color when lever is ON (base color, without HDR intensity).")]
    [SerializeField] private Color ledOnColor = new Color(1f, 0.5f, 0f); // Orange

    [Tooltip("LED color when this lever is the last one activated (for the player only).")]
    [SerializeField] private Color ledLatestOnColor = Color.red;

    [Tooltip("LED color when lever is OFF.")]
    [SerializeField] private Color ledOffColor = Color.green;

    [Tooltip("HDR intensity multiplier when LED is ON.")]
    [SerializeField] [Min(0f)] private float ledOnIntensity = 4f;

    [Tooltip("HDR intensity multiplier when LED is OFF.")]

    [SerializeField] [Min(0f)] private float ledOffIntensity = 2.5f;

    [Header("Animation !")]
    
    [SerializeField] [Min(0f)] private float rotationDuration = 0.2f;

    [Tooltip("Duration for LED color fading between two states.")]
    [SerializeField][Min(0f)] private float ledFadeDuration = 0.15f;

    [Header("Glitch Settings")]
    [Tooltip("Minimum delay between two glitch bursts (in seconds).")]
    [SerializeField][Min(0f)] private float glitchMinDelay = 2f;

    [Tooltip("Maximum delay between two glitch bursts (in seconds).")]
    [SerializeField][Min(0f)] private float glitchMaxDelay = 6f;

    [Tooltip("Minimum number of flashes during one glitch burst.")]
    [SerializeField][Min(1)] private int glitchMinFlashes = 1;

    [Tooltip("Maximum number of flashes during one glitch burst.")]
    [SerializeField][Min(1)] private int glitchMaxFlashes = 4;

    [Tooltip("Duration for a single glitch flash (fade to dark and back).")]
    [SerializeField][Min(0f)] private float glitchFlashDuration = 0.05f;

    [Header("Latest Lever Pulse")]
    [Tooltip("Speed of the continuous SIN pulse for the latest ON lever.")]
    [SerializeField][Min(0f)] private float latestPulseSpeed = 3f;

    [Tooltip("Minimum HDR multiplier for the SIN pulse (relative to ledOnIntensity).")]
    [SerializeField][Range(0f, 2f)] private float latestPulseMinMultiplier = 0.8f;

    [Tooltip("Maximum HDR multiplier for the SIN pulse (relative to ledOnIntensity).")]
    [SerializeField][Range(0f, 2f)] private float latestPulseMaxMultiplier = 1.3f;


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
    private bool isAnimating = false; // Simple flag to prevent spam clicks

    // Internal LED animation state (for color fading, glitch, latest lever pulse)
    private Color _currentEmissionColor;   // Last emission color used by the LED
    private Tween ledColorTween;           // Tween for color fading
    private bool isLatestOnLever = false;  // is latest activated lever ?
    private Coroutine glitchCoroutine;     // Glitch loop coroutine



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

    private void OnEnable()
    {
        glitchCoroutine = StartCoroutine(GlitchLoop());
    }

    private void OnDisable()
    {
        if (glitchCoroutine != null)
            StopCoroutine(glitchCoroutine);

        ledColorTween?.Kill();
    }

    private void Update()
    {
        // Continuous SIN pulse only when this lever is marked as the latest ON lever !
        if (isLatestOnLever && _state == LeverState.On && ledRenderer != null)
        {
            // Compute SIN-based intensity multiplier
            float sin = Mathf.Sin(Time.time * latestPulseSpeed);
            float t = 0.5f * (sin + 1f); // Map [-1,1] to [0,1]
            float pulseMultiplier = Mathf.Lerp(latestPulseMinMultiplier, latestPulseMaxMultiplier, t);

            float finalIntensity = ledOnIntensity * pulseMultiplier;
            Color emission = ledLatestOnColor * Mathf.LinearToGammaSpace(finalIntensity);

            // Apply the pulsed emission instantly (we are already in a continuous animation here)
            SetEmissionInstant(emission);
        }
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
        if (isAnimating) return; // Prevent spam clicks during animation

        // This is very useful contraction for little line like this:
        // State == if on, set it to off else on
        SetState(_state == LeverState.On ? LeverState.Off : LeverState.On);
    }

    /// <summary>
    /// Sets the lever state and updates visuals + events.
    /// </summary>
    /// <param name="newState">New state to apply.</param>
    /// <param name="invokeEvent">If true (by default): invokes the OnStateChanged event.</param>
    public void SetState(LeverState newState, bool invokeEvent = true)
    {
        isAnimating = true;

        // USE IT @ReAdam when developping the gameplay
        // Debug.Log($"[Lever] {name} of {owner} changing state from {_state} to {newState}");

        _state = newState;
        UpdateVisuals(invokeEvent);
    }

    /// <summary>
    /// Updates handle rotation and LED emission color based on the current state.
    /// When finished. Call the Unity Event "onStateChanged"
    /// </summary>
    private void UpdateVisuals(bool invokeEvent = true)
    {
        UpdateHandleRotation(invokeEvent);
        //UpdateLedEmission(); we we'll update it when the event is received by LeverRow
    }

    /// <summary>
    /// Rotates the handle transform to match the current state.
    /// </summary>
    private void UpdateHandleRotation(bool invokeEvent = true)
    {
        if (handleTransform == null)
            return;

        // Determine target Euler angles based on the current state
        Vector3 targetEuler = 
            _state == LeverState.On? onLocalEulerAngles : offLocalEulerAngles;

        // Set the local rotation to the target Euler angles
        handleTransform.DOLocalRotate(targetEuler, rotationDuration).SetEase(Ease.OutBack).SetTarget(this).OnComplete(() =>
        {
            // Animation is finished, invoke the event
            // I set it because we never know if we'll use it, but we can remove it later btw
            if (invokeEvent)
            {
                onStateChanged?.Invoke(this, _state);
            }

            isAnimating = false; // Reset the animation flag

            //Update led after the handle !
            UpdateLedEmission();
        });

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


        // Choose base color + intensity depending on state
        Color baseColor;
        float intensity;

        if (isLatestOnLever && _state == LeverState.On)
        {
            baseColor = ledLatestOnColor;
            intensity = ledOnIntensity;
        }
        else
        {
            // Determine color and intensity of the LED based on the current state
            switch (_state)
            {
                case LeverState.On:
                    baseColor = ledOnColor;
                    intensity = ledOnIntensity;
                    break;

                case LeverState.Off:
                default:
                    baseColor = ledOffColor;
                    intensity = ledOffIntensity;
                    break;
            }
        }

        // Convert linear intensity to gamma space for HDR emission.
        Color targetEmissionColor = baseColor * Mathf.LinearToGammaSpace(intensity);

        // Animate the LED emission from the current color to the target color
        TweenEmissionTo(targetEmissionColor, ledFadeDuration);
    }

    /// <summary>
    /// Apply the emission color directly to the MaterialPropertyBlock and cache it.
    /// </summary>
    private void SetEmissionInstant(Color emissionColor)
    {
        _currentEmissionColor = emissionColor;

        // Get the current MaterialPropertyBlock from the renderer
        ledRenderer.GetPropertyBlock(_mpb);

        // Set the emission color in the MaterialPropertyBlock
        _mpb.SetColor("_EmissionColor", emissionColor);
        // Apply the updated MaterialPropertyBlock to the LED renderer !
        ledRenderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// Starts a DOTween color tween from the current emission to the target emission.
    /// </summary>
    private void TweenEmissionTo(Color targetEmissionColor, float duration)
    {
        // Kill previous color tween if still running
        ledColorTween?.Kill();

        Color startColor = _currentEmissionColor;

        ledColorTween = DOVirtual.Color(startColor, targetEmissionColor, duration, c =>
        {
            // On each tween step, update the LED emission instantly with the interpolated color
            SetEmissionInstant(c);
        });
    }


    /// <summary>
    /// his lever becomes the latest activated: LED turns RED and starts pulsing.
    /// Called by the LeverRowManager when this lever becomes the latest ON lever.
    /// </summary>
    public void SetAsLatestOnLever()
    {
        isLatestOnLever = true;

        // Initial red (pulse will override)
        //Color startRed = ledLatestOnColor * Mathf.LinearToGammaSpace(ledOnIntensity);
        //SetEmissionInstant(startRed);
    }

    /// <summary>
    /// This lever is no longer the latest: back to normal LED color.
    /// Used when another lever becomes the latest activated.
    /// </summary>
    public void ClearLatestMarker()
    {
        isLatestOnLever = false;
        UpdateLedEmission();
    }


    /// <summary>
    /// Infinite loop that randomly triggers glitch bursts on this LED.
    /// All levers can glitch, regardless of their color (OFF/ON/latest).
    /// </summary>

    private IEnumerator GlitchLoop()
    {
        // Endless loop as long as this component is enabled
        while (true)
        {
            // Random delay between two glitch bursts
            float wait = UnityEngine.Random.Range(glitchMinDelay, glitchMaxDelay);
            yield return new WaitForSeconds(wait);

            // Random number of flashes for this burst
            int flashes = UnityEngine.Random.Range(glitchMinFlashes, glitchMaxFlashes + 1);

            for (int i = 0; i < flashes; i++)
            {
                // Fade quickly to dark
                TweenEmissionTo(Color.black, glitchFlashDuration * 0.5f);

                yield return new WaitForSeconds(glitchFlashDuration * 0.5f);

                // Fade back to the proper state color (OFF/ON/latest)
                UpdateLedEmission();
                yield return new WaitForSeconds(glitchFlashDuration * 0.5f);
            }
        }
    }



}
