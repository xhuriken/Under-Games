using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum InteractionType
{
    MoveTo = 1,
    Object = 2
}

public class PointClickTarget : MonoBehaviour
{

    [Header("Point & Click")]
    [Tooltip("Defines what should happen when the player clicks this object.")]
    [SerializeField] private InteractionType interactionType = InteractionType.MoveTo;

    [Tooltip("Optional target the player should move to.")]
    [SerializeField] private Transform moveTarget;

    [Tooltip("Event called when this object is used")]
    [SerializeField] private UnityEvent onUse;

    [Tooltip("All the outlines script for this object")]
    [SerializeField] private Outline[] outlines;

    [Header("Rules")]
    [Tooltip("If set, player must already be on this anchor to interact with this target.")]
    [SerializeField] private PointClickTarget requiredAnchor;
    [Tooltip("If true, when player move to this object, it convert to an usable object and vise versa when he leave it")]
    [SerializeField] private bool isConvertible = false;
    public PointClickTarget RequiredAnchor => requiredAnchor;

    /// <summary>
    /// Called by the point & click controller when the player clicks on this object.
    /// The controller is responsible for moving the player, or calling Use(),
    /// depending on the interactionType.
    /// </summary>
    public InteractionType InteractionType => interactionType;

    /// <summary>
    /// Returns the world position the player should move to.
    /// </summary>
    public Transform GetTransformTarget()
    {
        return moveTarget;
    }

    /// <summary>
    /// Called when the object is "used"
    /// And it's the spefific object script who will define what to do when used ! (it will subscribe)
    /// </summary>
    public void Use()
    {
        onUse?.Invoke();
    }

    /// <summary>
    /// Set Outlines scripts of this object to specific value
    /// </summary>
    /// <param name="enable"></param>
    public void SetOutline(bool enable)
    {
        if (outlines == null || outlines.Length == 0) return;

        foreach (var o in outlines)
        {
            o.enabled = enable;
        }
    }

    /// <summary>
    /// Convert "MoveTo" to an object and vice versa!
    /// </summary>
    public void Convert()
    {
        if(!isConvertible) return;

        interactionType = interactionType == InteractionType.MoveTo ? InteractionType.Object : InteractionType.MoveTo;
    }

    private IEnumerator Start()
    {
        // Force refresh of QuickOutline (otherwise the outline color is white the first time)
        SetOutline(true);
        // Ensure all plugin Start/Awake ran
        yield return null;

        SetOutline(false);  
    }

}
