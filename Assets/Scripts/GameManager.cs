using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Selection Info")]
    public string selectedSaveFile;
    public string selectedBoss;
    public string selectedCombatStyle;
    public GameObject[] CombatStylePrefabs; // assign in Inspector

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
}
