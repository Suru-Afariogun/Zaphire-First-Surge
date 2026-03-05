using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    /// <summary>Default number of lives when starting a new game.</summary>
    public const int DefaultLives = 3;

    [Header("Selection Info")]
    public string selectedSaveFile;
    public string selectedBoss;
    public string selectedCombatStyle;
    public GameObject[] CombatStylePrefabs; // assign in Inspector

    [Header("Global UI Prefabs")]
    [Tooltip("Game Over popup prefab (the panel only — no Canvas). Spawned as a child of the scene's Game Over Canvas.")]
    public GameObject gameOverPopupPrefab;
    [Tooltip("Name of the Canvas in each gameplay scene that should hold the Game Over popup. Create a Canvas with this name in boss fight (and other) scenes.")]
    public string gameOverCanvasName = "Game Over Canvas";

    [Tooltip("Pause popup prefab (the panel only — no Canvas). Spawned as a child of the scene's Pause Canvas in gameplay scenes.")]
    public GameObject pausePopupPrefab;
    [Tooltip("Name of the Canvas in each gameplay scene that should hold the Pause popup.")]
    public string pauseCanvasName = "Pause Canvas";

    /// <summary>Current lives in this play session; used when saving and restored when loading a slot.</summary>
    public int currentLives = DefaultLives;

    /// <summary>Name of the gameplay scene we are in (or last left); used when saving progress to a slot.</summary>
    public string currentSceneName;

    /// <summary>Boss names the player has beaten this session; persisted when saving to a slot.</summary>
    private List<string> _beatenBossesThisSession = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Also run once for the very first scene after GameManager is created.
        TrySpawnGameOverPopupForCurrentScene();
        TrySpawnPausePopupForCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TrySpawnGameOverPopupForCurrentScene();
        TrySpawnPausePopupForCurrentScene();
    }

    /// <summary>
    /// Spawns the Game Over popup under this scene's "Game Over Canvas" (no Canvas in the prefab).
    /// Called on Start (for first scene) and on every sceneLoaded.
    /// </summary>
    private void TrySpawnGameOverPopupForCurrentScene()
    {
        var existing = Object.FindFirstObjectByType<GameOverPopupUI>();
        if (gameOverPopupPrefab == null || existing != null) return;

        GameObject canvasGO = GameObject.Find(gameOverCanvasName);
        if (canvasGO == null)
        {
            Debug.LogWarning("[GameManager] No GameObject named \"" + gameOverCanvasName + "\" in this scene. Add a Canvas with that name to show the Game Over popup.");
            return;
        }

        GameObject popup = Instantiate(gameOverPopupPrefab, canvasGO.transform, false);

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
        }

        Debug.Log("[GameManager] Game Over popup instantiated under " + gameOverCanvasName + ".");
    }

    /// <summary>
    /// Spawns the Pause popup under this scene's "Pause Canvas" (no Canvas in the prefab).
    /// Called on Start (for first scene) and on every sceneLoaded.
    /// </summary>
    private void TrySpawnPausePopupForCurrentScene()
    {
        var existing = Object.FindFirstObjectByType<PausePopupUI>();
        if (pausePopupPrefab == null || existing != null) return;

        GameObject canvasGO = GameObject.Find(pauseCanvasName);
        if (canvasGO == null)
        {
            Debug.LogWarning("[GameManager] No GameObject named \"" + pauseCanvasName + "\" in this scene. Add a Canvas with that name to show the Pause popup.");
            return;
        }

        GameObject popup = Instantiate(pausePopupPrefab, canvasGO.transform, false);

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        if (popupRect != null)
        {
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.offsetMin = Vector2.zero;
            popupRect.offsetMax = Vector2.zero;
        }

        Debug.Log("[GameManager] Pause popup instantiated under " + pauseCanvasName + ".");
    }

    /// <summary>
    /// Sets the default combat style prefab (first one in the array) if none is selected
    /// </summary>
    public void SetDefaultCombatStyle()
    {
        if (CombatStylePrefabs != null && CombatStylePrefabs.Length > 0)
        {
            selectedCombatStyle = CombatStylePrefabs[0].name;
            Debug.Log("Default combat style set to: " + selectedCombatStyle);
        }
        else
        {
            Debug.LogWarning("No combat style prefabs assigned in GameManager!");
        }
    }

    public GameObject GetSelectedCombatStylePrefab()
    {
        // If no combat style is selected, use the default (first prefab)
        if (string.IsNullOrEmpty(selectedCombatStyle))
        {
            SetDefaultCombatStyle();
        }

        // Try to find the selected combat style prefab
        if (!string.IsNullOrEmpty(selectedCombatStyle))
        {
            foreach (GameObject prefab in CombatStylePrefabs)
            {
                if (prefab != null && prefab.name == selectedCombatStyle)
                    return prefab;
            }
            Debug.LogWarning("No matching prefab found for: " + selectedCombatStyle);
        }

        // Fallback: return the first prefab if available
        if (CombatStylePrefabs != null && CombatStylePrefabs.Length > 0 && CombatStylePrefabs[0] != null)
        {
            Debug.LogWarning("Using default prefab as fallback");
            return CombatStylePrefabs[0];
        }

        Debug.LogError("No combat style prefabs available!");
        return null;
    }

    /// <summary>
    /// Saves the current scene name for the selected save file
    /// </summary>
    public void SaveCurrentScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(selectedSaveFile))
        {
            string key = GetSaveFileSceneKey(selectedSaveFile);
            PlayerPrefs.SetString(key, sceneName);
            PlayerPrefs.Save();
            Debug.Log("Saved scene '" + sceneName + "' for " + selectedSaveFile);
        }
    }

    /// <summary>
    /// Gets the saved scene name for a specific save file
    /// </summary>
    public string GetSavedScene(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName))
            return null;

        string key = GetSaveFileSceneKey(saveFileName);
        string sceneName = PlayerPrefs.GetString(key, "");
        
        // Return null if no save found (empty string means no save)
        return string.IsNullOrEmpty(sceneName) ? null : sceneName;
    }

    /// <summary>
    /// Checks if a save file has a saved scene
    /// </summary>
    public bool HasSaveFile(string saveFileName)
    {
        return !string.IsNullOrEmpty(GetSavedScene(saveFileName));
    }

    /// <summary>
    /// Gets the PlayerPrefs key for a save file's scene
    /// </summary>
    private string GetSaveFileSceneKey(string saveFileName)
    {
        return saveFileName.Replace(" ", "_") + "_LastScene";
    }

    /// <summary>Returns the PlayerPrefs key used to store lives for the given save slot.</summary>
    private string GetSaveFileLivesKey(string saveFileName)
    {
        return saveFileName.Replace(" ", "_") + "_Lives";
    }

    /// <summary>Returns the PlayerPrefs key used to store beaten boss names (comma-separated) for the given save slot.</summary>
    private string GetSaveFileBeatenBossesKey(string saveFileName)
    {
        return saveFileName.Replace(" ", "_") + "_BeatenBosses";
    }

    /// <summary>Gets the saved lives count for a save file; returns DefaultLives if never saved.</summary>
    public int GetSavedLives(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return DefaultLives;
        return PlayerPrefs.GetInt(GetSaveFileLivesKey(saveFileName), DefaultLives);
    }

    /// <summary>Writes the lives count for a save file to PlayerPrefs.</summary>
    public void SetSavedLives(string saveFileName, int lives)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;
        PlayerPrefs.SetInt(GetSaveFileLivesKey(saveFileName), lives);
        PlayerPrefs.Save();
    }

    /// <summary>Gets the list of boss names the player has beaten for this save file.</summary>
    public string[] GetBeatenBosses(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return new string[0];
        string raw = PlayerPrefs.GetString(GetSaveFileBeatenBossesKey(saveFileName), "");
        if (string.IsNullOrEmpty(raw)) return new string[0];
        return raw.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Stores the list of beaten boss names for this save file.</summary>
    public void SetBeatenBosses(string saveFileName, IEnumerable<string> bossNames)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;
        string raw = bossNames != null ? string.Join(",", bossNames) : "";
        PlayerPrefs.SetString(GetSaveFileBeatenBossesKey(saveFileName), raw);
        PlayerPrefs.Save();
    }

    /// <summary>Records that the player beat this boss this session; used when saving to a slot.</summary>
    public void RecordBossDefeated(string bossName)
    {
        if (string.IsNullOrEmpty(bossName)) return;
        if (!_beatenBossesThisSession.Contains(bossName))
            _beatenBossesThisSession.Add(bossName);
    }

    /// <summary>Returns the list of boss names beaten in the current session (for saving to a slot).</summary>
    public List<string> GetBeatenBossesThisSession()
    {
        return new List<string>(_beatenBossesThisSession);
    }

    /// <summary>Clears the list of bosses beaten this session (e.g. when starting a new game).</summary>
    public void ClearBeatenBossesThisSession()
    {
        _beatenBossesThisSession.Clear();
    }

    /// <summary>Loads the save file's lives and beaten bosses into runtime and sets selectedSaveFile. Call before loading the scene when continuing.</summary>
    public void LoadSaveFileState(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;
        selectedSaveFile = saveFileName;
        currentLives = GetSavedLives(saveFileName);
        _beatenBossesThisSession = new List<string>(GetBeatenBosses(saveFileName));
    }

    /// <summary>Saves current scene name, lives, and beaten bosses to the given slot and sets it as the selected save file.</summary>
    public void SaveFullProgressToSlot(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;
        selectedSaveFile = saveFileName;
        string keyScene = GetSaveFileSceneKey(saveFileName);
        PlayerPrefs.SetString(keyScene, currentSceneName ?? "");
        PlayerPrefs.SetInt(GetSaveFileLivesKey(saveFileName), currentLives);
        SetBeatenBosses(saveFileName, _beatenBossesThisSession);
        PlayerPrefs.Save();
        Debug.Log("Saved progress to " + saveFileName + ": scene=" + currentSceneName + ", lives=" + currentLives);
    }

    // ============================================
    // COMBAT STYLE CYCLING (weapon wheel style)
    // ============================================

    /// <summary>
    /// Cycles to the next combat style in the array (wraps around)
    /// </summary>
    public void CycleToNextCombatStyle()
    {
        if (CombatStylePrefabs == null || CombatStylePrefabs.Length == 0)
        {
            Debug.LogWarning("No combat styles available to cycle!");
            return;
        }

        // Find current index
        int currentIndex = GetCurrentCombatStyleIndex();
        
        // Move to next (wrap around)
        int nextIndex = (currentIndex + 1) % CombatStylePrefabs.Length;
        
        // Set the new combat style
        if (CombatStylePrefabs[nextIndex] != null)
        {
            selectedCombatStyle = CombatStylePrefabs[nextIndex].name;
            Debug.Log("Switched to combat style: " + selectedCombatStyle);
        }
    }

    /// <summary>
    /// Cycles to the previous combat style in the array (wraps around)
    /// </summary>
    public void CycleToPreviousCombatStyle()
    {
        if (CombatStylePrefabs == null || CombatStylePrefabs.Length == 0)
        {
            Debug.LogWarning("No combat styles available to cycle!");
            return;
        }

        // Find current index
        int currentIndex = GetCurrentCombatStyleIndex();
        
        // Move to previous (wrap around)
        int previousIndex = currentIndex - 1;
        if (previousIndex < 0)
            previousIndex = CombatStylePrefabs.Length - 1;
        
        // Set the new combat style
        if (CombatStylePrefabs[previousIndex] != null)
        {
            selectedCombatStyle = CombatStylePrefabs[previousIndex].name;
            Debug.Log("Switched to combat style: " + selectedCombatStyle);
        }
    }

    /// <summary>
    /// Gets the current index of the selected combat style in the array
    /// </summary>
    private int GetCurrentCombatStyleIndex()
    {
        // Ensure we have a selected style
        if (string.IsNullOrEmpty(selectedCombatStyle))
        {
            SetDefaultCombatStyle();
        }

        // Find the index
        for (int i = 0; i < CombatStylePrefabs.Length; i++)
        {
            if (CombatStylePrefabs[i] != null && CombatStylePrefabs[i].name == selectedCombatStyle)
            {
                return i;
            }
        }

        // If not found, return 0 (first style)
        return 0;
    }
}
