using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to each save slot button. Set Slot Index (0 = first slot, 1 = second, etc.),
/// then in the Button's On Click () call this component's OnClick. The slot name is read
/// from SaveFileSelectManager so changing the number of slots in the manager is enough.
/// </summary>
public class SaveSlotButton : MonoBehaviour
{
    [Tooltip("TEMPORARY: If on, clicking this slot just loads Boss Select. Turn off when save/load is fixed.")]
    [SerializeField] private bool tempSkipToBossSelect = true;

    [Tooltip("0 = first slot, 1 = second slot, etc. Must match the order in SaveFileSelectManager's save file slots.")]
    [SerializeField] private int slotIndex;

    /// <summary>Call this from the Button's On Click () list. No parameters needed.</summary>
    public void OnClick()
    {
        Debug.Log("SaveSlotButton.OnClick called");
        if (tempSkipToBossSelect)
        {
            Debug.Log("Loading Boss Select Screen...");
            SceneManager.LoadScene("Boss Select Screen");
            return;
        }
        var ui = FindFirstObjectByType<SaveFileSelectUI>();
        if (ui != null)
            ui.OnSlotClickedByIndex(slotIndex);
    }
}
