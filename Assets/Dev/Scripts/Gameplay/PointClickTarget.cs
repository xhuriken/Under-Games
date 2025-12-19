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

    public void SetOutline(bool enable)
    {
        if (outlines == null && outlines.Length == 0f) return;

        foreach (var o in outlines)
        {
            o.enabled = enable;
        }
    }

    private void Awake()
    {
        // Disable all outline
        if (outlines == null && outlines.Length == 0f) return;

        foreach (var o in outlines)
        {
            o.enabled = false;
        }
    }
}
