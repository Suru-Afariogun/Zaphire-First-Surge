using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single source of truth for save file slots, save/load mode, and "select save file then load scene" flow.
/// Use this from SaveFileSelectUI and MainMenu instead of hardcoding slot names or duplicating logic.
/// </summary>
public class SaveFileSelectManager : MonoBehaviour
{
    public static SaveFileSelectManager Instance { get; private set; }

    /// <summary>True when the screen is in "save to slot" mode; false when in "load from slot" mode.</summary>
    public bool IsSaveMode { get; private set; } = true;

    [Header("Save file slots")]
    [Tooltip("Display names for each save slot. Used for selection and PlayerPrefs keys.")]
    [SerializeField] private string[] _saveFileSlotNames = {
        "Save File 1", "Save File 2", "Save File 3", "Save File 4",
        "Save File 5", "Save File 6", "Save File 7", "Save File 8"
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optional: uncomment if this should persist across scenes like GameManager
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>All save file slot names. Use for UI and iteration.</summary>
    public string[] GetSaveFileSlotNames()
    {
        return _saveFileSlotNames;
    }

    /// <summary>Whether the given slot has any saved data.</summary>
    public bool HasSaveData(string saveFileName)
    {
        return GameManager.Instance != null && GameManager.Instance.HasSaveFile(saveFileName);
    }

    /// <summary>Saved scene name for the slot, or null if empty.</summary>
    public string GetSavedScene(string saveFileName)
    {
        return GameManager.Instance != null ? GameManager.Instance.GetSavedScene(saveFileName) : null;
    }

    /// <summary>Gets the saved lives count for the given slot; returns default (e.g. 3) if no save.</summary>
    public int GetLives(string saveFileName)
    {
        return GameManager.Instance != null ? GameManager.Instance.GetSavedLives(saveFileName) : GameManager.DefaultLives;
    }

    /// <summary>Gets the list of boss names the player has beaten for this save slot.</summary>
    public string[] GetBeatenBosses(string saveFileName)
    {
        return GameManager.Instance != null ? GameManager.Instance.GetBeatenBosses(saveFileName) : new string[0];
    }

    /// <summary>Puts the screen into save mode: clicking a slot will show confirm and save progress to that slot.</summary>
    public void EnterSaveMode()
    {
        IsSaveMode = true;
    }

    /// <summary>Puts the screen into load mode: clicking a slot will load that save.</summary>
    public void EnterLoadMode()
    {
        IsSaveMode = false;
    }

    /// <summary>
    /// Saves current progress (scene, lives, beaten bosses) to the given slot. Call after user confirms in the popup.
    /// </summary>
    public void SaveProgressToSlot(string saveFileName)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.SaveFullProgressToSlot(saveFileName);
    }

    /// <summary>
    /// Selects this save file and loads the Boss Select scene (new game flow).
    /// Resets lives to default and clears beaten bosses for the new game.
    /// </summary>
    public void SelectSaveFileAndGoToBossSelect(string saveFileName)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.selectedSaveFile = saveFileName;
        GameManager.Instance.currentLives = GameManager.DefaultLives;
        GameManager.Instance.currentSceneName = "";
        GameManager.Instance.ClearBeatenBossesThisSession();
        SceneManager.LoadScene("Boss Select Screen");
    }

    /// <summary>
    /// For an empty slot: creates initial save data (Boss Select scene, default lives, no beaten bosses),
    /// then loads the Boss Select screen. Use when the player clicks an empty slot in Load mode.
    /// </summary>
    public void InitializeEmptySlotAndGoToBossSelect(string saveFileName)
    {
        if (GameManager.Instance == null) return;
        const string bossSelectScene = "Boss Select Screen";
        GameManager.Instance.selectedSaveFile = saveFileName;
        GameManager.Instance.currentLives = GameManager.DefaultLives;
        GameManager.Instance.currentSceneName = bossSelectScene;
        GameManager.Instance.ClearBeatenBossesThisSession();
        GameManager.Instance.SaveFullProgressToSlot(saveFileName);
        SceneManager.LoadScene(bossSelectScene);
    }

    /// <summary>
    /// Tries to continue from the given save file. Loads that slot's state (lives, beaten bosses) then loads the saved scene.
    /// Returns true if a scene was loaded.
    /// </summary>
    public bool TryContinueFromSaveFile(string saveFileName)
    {
        string scene = GetSavedScene(saveFileName);
        if (string.IsNullOrEmpty(scene)) return false;
        // Load lives and beaten bosses into GameManager so gameplay uses them
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSaveFileState(saveFileName);
            GameManager.Instance.currentSceneName = scene;
        }
        SceneManager.LoadScene(scene);
        return true;
    }

    /// <summary>
    /// Finds the first save file with data and continues from it. Returns true if a scene was loaded.
    /// </summary>
    public bool TryContinueFromLatestSave()
    {
        foreach (string slot in _saveFileSlotNames)
        {
            if (TryContinueFromSaveFile(slot))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Loads the Save File Select scene (e.g. when no save found on Continue).
    /// </summary>
    public void LoadSaveFileSelectScene()
    {
        SceneManager.LoadScene("Save File Select Screen");
    }
}
