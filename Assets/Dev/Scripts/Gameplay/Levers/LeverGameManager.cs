using UnityEngine;

/// <summary>
/// Central manager for lever logic: checks states, handles items, BlaBlaBla.
/// Move it in a global game manager ? or use this IN the global game manager ?
/// Make this singleton ? I don't know yet...
/// </summary>
public class LeverGameManager : MonoBehaviour
{
    [Header("Lever Rows")]
    [SerializeField] private LeverRow playerRow;
    [SerializeField] private LeverRow opponentRow;

    private void OnEnable()
    {
        RegisterLeverEvents(playerRow);
        RegisterLeverEvents(opponentRow);
    }

    private void OnDisable()
    {
        UnregisterLeverEvents(playerRow);
        UnregisterLeverEvents(opponentRow);
    }

    /// <summary>
    /// Subscribes to state change events for all levers in a row.
    /// </summary>
    private void RegisterLeverEvents(LeverRow row)
    {
        if (row == null) return;

        foreach (var lever in row.Levers)
        {
            if (lever == null) continue;
            // Subscribe
            lever.OnStateChanged.AddListener(OnLeverStateChanged);
        }
    }

    /// <summary>
    /// Unsubscribes from state change events for all levers in a row.
    /// </summary>
    private void UnregisterLeverEvents(LeverRow row)
    {
        if (row == null) return;

        foreach (var lever in row.Levers)
        {
            if (lever == null) continue;
            // Unsubscribe
            lever.OnStateChanged.RemoveListener(OnLeverStateChanged);
        }
    }

    /// <summary>
    /// Handler called whenever any lever changes state.
    /// Check if someone died, trigger SFX...
    /// </summary>
    private void OnLeverStateChanged(Lever lever, LeverState state)
    {
        // Just a debug log for now ! Waiting for de Adam's logic !! <3
        Debug.Log($"[LeverGameManager] Lever '{lever.name}' changed ! {lever.Owner} is now {state}");
    }

    /// <summary>
    /// Called when someone use an item that swaps a lever from the player row
    /// and the lever from the opponent row.
    /// </summary>
    /// <param name="leverIndex">Index of the lever to affect in both rows.</param>
    public void UseSwapItem(int leverIndex)
    {
        // For now i didn't made the opponentRow sooooo he is null :3
        if (playerRow == null || opponentRow == null)
            return;

        // Take the lever of Player AND Opponent at the given index
        Lever playerLever = playerRow.GetLever(leverIndex);
        Lever opponentLever = opponentRow.GetLever(leverIndex);

        // Invert it ! (NEED MORE LOGIC !)
        // If they are Off twice before toggle it, make an different effect etc etc etc my bite
        if (playerLever != null)
            playerLever.Toggle();

        if (opponentLever != null)
            opponentLever.Toggle();
    }
}