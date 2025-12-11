using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Represents a row of levers (The player row, or Oppenent)
/// </summary>
public class LeverRow : MonoBehaviour
{
    [Tooltip("Owner of this row (player or opponent).")]
    [SerializeField] private LeverOwner owner = LeverOwner.Player;

    [Tooltip("All levers in this row. If empty, can be auto-filled from children.")]
    [SerializeField] private List<Lever> levers = new List<Lever>();

    private Lever latestActivatedLever;
    /// <summary>
    /// Owner of this row.
    /// </summary>
    public LeverOwner Owner => owner;

    /// <summary>
    /// Read-only access to the levers in this row.
    /// </summary>
    public IReadOnlyList<Lever> Levers => levers;

    private void OnValidate()
    {
        // OPTIONAL : Get all levers from children if none assigned
        if (levers == null || levers.Count == 0)
        {
            // Include also the inactive ones ! (in the inspector you know)
            levers = new List<Lever>(GetComponentsInChildren<Lever>(includeInactive: true));
        }

        foreach (Lever lever in levers)
        {
            if (lever == null) continue;
            // For all the levers in the row, subscribe to their state change event
            // (and call OnLeverStateChanged if he is called)
            lever.OnStateChanged.AddListener(OnLeverStateChanged);
        }
    }

    /// <summary>
    /// Called whenever any lever in the row changes state.
    /// Handles the logic of the "latest activated lever".
    /// </summary>
    private void OnLeverStateChanged(Lever lever, LeverState newState)
    {
        // If lever is OFF do nothing
        if (newState == LeverState.Off)
            return;

        // If there was a previously highlighted lever we reset it
        if (latestActivatedLever != null && latestActivatedLever != lever)
            latestActivatedLever.ClearLatestMarker();

        // set the new one
        latestActivatedLever = lever;
        //Update to RED !
        latestActivatedLever.SetAsLatestOnLever();
    }


    /// <summary>
    /// Returns the lever at the given index, or null if out of range.
    /// </summary>
    public Lever GetLever(int index)
    {
        if (index < 0 || index >= levers.Count)
            return null;

        return levers[index];
    }

    /// <summary>
    /// HELPER FOR FUTURE ?
    /// Set all levers in this row to ON or OFF.
    /// </summary>
    public void SetAll(LeverState state)
    {
        foreach (var lever in levers)
        {
            if (lever == null) continue;
            lever.SetState(state, false);
        }
    }

    /// <summary>
    /// HELPER FOR FUTURE ?
    /// Returns the current states of all levers as a bool array (true = ON).
    /// </summary>
    public bool[] GetStatesAsBoolArray()
    {
        bool[] result = new bool[levers.Count];
        for (int i = 0; i < levers.Count; i++)
        {
            result[i] = levers[i] != null && levers[i].State == LeverState.On;
        }

        return result;
    }
}