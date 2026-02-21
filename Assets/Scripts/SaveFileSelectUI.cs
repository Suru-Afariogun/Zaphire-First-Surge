using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI controller for the Save File Select screen: Save/Load mode, slot actions,
/// character-and-lives box, confirm-save popup, and boss profile display.
/// </summary>
public class SaveFileSelectUI : MonoBehaviour
{
    [Header("Save / Load mode buttons")]
    [Tooltip("Button that switches the screen into Save mode (clicking a slot saves progress there).")]
    [SerializeField] private Button saveModeButton;
    [Tooltip("Button that switches the screen into Load mode (clicking a slot loads that save).")]
    [SerializeField] private Button loadModeButton;

    [Header("Character and lives box (widget)")]
    [Tooltip("Shows the number of lives for the save slot currently being previewed/selected. Use a TextMeshPro - Text (UI) component.")]
    [SerializeField] private TMP_Text livesCountText;
    [Tooltip("Optional: container that holds the character model; can be left unassigned if you only show lives.")]
    [SerializeField] private GameObject characterModelContainer;

    [Header("Confirm save popup")]
    [Tooltip("Panel shown when the player clicks a slot in Save mode. Keep it disabled in the editor; the script will show it only when needed.")]
    [SerializeField] private GameObject confirmSavePopup;
    [Tooltip("Confirm button inside the popup that triggers saving to the pending slot.")]
    [SerializeField] private Button confirmSaveButton;
    [Tooltip("Cancel button that closes the popup without saving.")]
    [SerializeField] private Button cancelSaveButton;
    [Tooltip("Optional: text that shows which slot will be overwritten (e.g. 'Save to Save File 1?'). Use a TextMeshPro - Text (UI) component.")]
    [SerializeField] private TMP_Text confirmSaveMessageText;

    [Header("Boss profiles")]
    [Tooltip("Boss names in the same order as the profile images. Use the EXACT same strings as your Boss Select screen buttons (e.g. \"Bubble Blum\" or \"BubbleBlum\") so beaten state matches.")]
    [SerializeField] private string[] bossDisplayNames = new string[0];
    [Tooltip("Profile images for each boss; beaten bosses are shown with a darker tint.")]
    [SerializeField] private Image[] bossProfileImages = new Image[0];

    /// <summary>Slot the user is currently previewing; the character/lives box and boss profiles show this slot's data.</summary>
    private string _previewSlot;
    /// <summary>When in Save mode and user clicked a slot, we store it here until they confirm or cancel the popup.</summary>
    private string _pendingSaveSlot;

    private void Start()
    {
        // Save button: put screen in Save mode so clicking a slot asks to confirm and save progress there
        if (saveModeButton != null)
            saveModeButton.onClick.AddListener(OnSaveModeClicked);

        // Load button: put screen in Load mode so clicking a slot loads that save (or starts new game if empty)
        if (loadModeButton != null)
            loadModeButton.onClick.AddListener(OnLoadModeClicked);

        // Popup Confirm: save progress to the chosen slot then hide the popup
        if (confirmSaveButton != null)
            confirmSaveButton.onClick.AddListener(OnConfirmSaveClicked);
        // Popup Cancel: hide popup without saving
        if (cancelSaveButton != null)
            cancelSaveButton.onClick.AddListener(OnCancelSaveClicked);

        // Keep confirm popup hidden until the player clicks a slot in Save mode (then we enable it)
        if (confirmSavePopup != null)
            confirmSavePopup.SetActive(false);
        _previewSlot = null;
        _pendingSaveSlot = null;

        // Preview the first slot by default so the character/lives box and boss profiles show something
        if (SaveFileSelectManager.Instance != null && SaveFileSelectManager.Instance.GetSaveFileSlotNames().Length > 0)
            SelectPreviewSlot(SaveFileSelectManager.Instance.GetSaveFileSlotNames()[0]);
    }

    /// <summary>Called when the player clicks the Save button at the top; switches to Save mode.</summary>
    private void OnSaveModeClicked()
    {
        if (SaveFileSelectManager.Instance == null) return;
        SaveFileSelectManager.Instance.EnterSaveMode();
    }

    /// <summary>Called when the player clicks the Load button at the top; switches to Load mode.</summary>
    private void OnLoadModeClicked()
    {
        if (SaveFileSelectManager.Instance == null) return;
        SaveFileSelectManager.Instance.EnterLoadMode();
    }

    /// <summary>Sets which save slot is being previewed and refreshes the character/lives box and boss profiles.</summary>
    private void SelectPreviewSlot(string saveFileName)
    {
        _previewSlot = saveFileName;
        RefreshCharacterAndLivesBox();
        RefreshBossProfiles();
    }

    /// <summary>Updates the character-and-lives box to show "current / max" (e.g. 2 / 3 after one death, 3 / 3 for empty/new slot).</summary>
    private void RefreshCharacterAndLivesBox()
    {
        if (livesCountText == null) return;
        int maxLives = GameManager.DefaultLives;
        int lives;
        if (SaveFileSelectManager.Instance == null)
        {
            lives = maxLives;
        }
        else
        {
            // Empty slot or no preview = default 3/3; otherwise use that slot's saved lives
            lives = string.IsNullOrEmpty(_previewSlot)
                ? maxLives
                : SaveFileSelectManager.Instance.GetLives(_previewSlot);
        }
        livesCountText.text = lives + " / " + maxLives;
    }

    /// <summary>Updates boss profile images: beaten bosses for the previewed slot are shown in a darker shade.</summary>
    private void RefreshBossProfiles()
    {
        if (SaveFileSelectManager.Instance == null || bossProfileImages == null) return;
        // Build set of boss names beaten in this slot (saved when player saves after beating a boss)
        HashSet<string> beaten = new HashSet<string>();
        if (!string.IsNullOrEmpty(_previewSlot))
        {
            foreach (string name in SaveFileSelectManager.Instance.GetBeatenBosses(_previewSlot))
                beaten.Add(name);
        }
        // Darken profile image for beaten bosses; leave others at full brightness
        for (int i = 0; i < bossProfileImages.Length && i < bossDisplayNames.Length; i++)
        {
            if (bossProfileImages[i] == null) continue;
            bool isBeaten = beaten.Contains(bossDisplayNames[i]);
            bossProfileImages[i].color = isBeaten ? new Color(0.5f, 0.5f, 0.5f, 1f) : Color.white;
        }
    }

    /// <summary>Called when the player clicks a save slot: in Save mode shows confirm popup; in Load mode loads or starts new game.</summary>
    public void OnSlotClicked(string saveFileName)
    {
        if (SaveFileSelectManager.Instance == null) return;

        // Refresh the character/lives box and boss profiles to show this slot's data
        SelectPreviewSlot(saveFileName);

        if (SaveFileSelectManager.Instance.IsSaveMode)
        {
            // Save mode: remember slot and show "Confirm save?" popup
            _pendingSaveSlot = saveFileName;
            if (confirmSaveMessageText != null)
                confirmSaveMessageText.text = "Save your progress to " + saveFileName + "?";
            if (confirmSavePopup != null)
                confirmSavePopup.SetActive(true);
        }
        else
        {
            // Load mode: slot with data -> load that save; empty slot -> create save data and load Boss Select
            if (SaveFileSelectManager.Instance.HasSaveData(saveFileName))
                SaveFileSelectManager.Instance.TryContinueFromSaveFile(saveFileName);
            else
                SaveFileSelectManager.Instance.InitializeEmptySlotAndGoToBossSelect(saveFileName);
        }
    }

    /// <summary>Called when the user confirms saving in the popup; writes progress to the pending slot and closes the popup.</summary>
    private void OnConfirmSaveClicked()
    {
        if (string.IsNullOrEmpty(_pendingSaveSlot) || SaveFileSelectManager.Instance == null) return;
        SaveFileSelectManager.Instance.SaveProgressToSlot(_pendingSaveSlot);
        _pendingSaveSlot = null;
        if (confirmSavePopup != null)
            confirmSavePopup.SetActive(false);
        // Refresh preview so the box shows updated lives/bosses for this slot
        SelectPreviewSlot(_previewSlot);
    }

    /// <summary>Called when the user cancels the save popup; closes the popup without saving.</summary>
    private void OnCancelSaveClicked()
    {
        _pendingSaveSlot = null;
        if (confirmSavePopup != null)
            confirmSavePopup.SetActive(false);
    }

    // --- Slot button: use SaveSlotButton component on each button and wire its OnClick to the button; one method works for any number of slots ---

    /// <summary>Called by SaveSlotButton when a slot is clicked. Index is 0-based; slot name comes from SaveFileSelectManager so slot count can change.</summary>
    public void OnSlotClickedByIndex(int index)
    {
        if (SaveFileSelectManager.Instance == null) return;
        string[] names = SaveFileSelectManager.Instance.GetSaveFileSlotNames();
        if (names == null || index < 0 || index >= names.Length) return;
        OnSlotClicked(names[index]);
    }

    /// <summary>Optional: call when the player hovers over a slot to preview it (e.g. from EventTrigger).</summary>
    public void OnSlotHover(string saveFileName)
    {
        SelectPreviewSlot(saveFileName);
    }

    /// <summary>TEMPORARY: Loads Boss Select Screen. Wire a button to this to skip save/load flow. Remove when done testing.</summary>
    public void TempGoToBossSelect()
    {
        SceneManager.LoadScene("Boss Select Screen");
    }
}
