using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Boss Select screen: loads boss fight scenes when a boss is clicked,
/// and handles navigation back to Main Menu or Save Select.
/// Attach to a GameObject in the Boss Select scene (e.g. Canvas or a controller object).
/// </summary>
public class BossSelectUI : MonoBehaviour
{
    [Header("Boss buttons: map each boss name to its fight scene")]
    [Tooltip("Display names shown on the boss buttons (e.g. 'Bubble Blum').")]
    [SerializeField] private string[] bossDisplayNames = { "Bubble Blum" };
    [Tooltip("Scene names for each boss fight, in the same order as bossDisplayNames.")]
    [SerializeField] private string[] bossSceneNames = { "Bubble Blum Boss Fight" };

    [Header("Navigation scene names")]
    [Tooltip("Scene to load when the player clicks 'Back to Main Menu'.")]
    [SerializeField] private string mainMenuSceneName = "Title Screen";
    [Tooltip("Scene to load when the player clicks 'Back to Save Select'.")]
    [SerializeField] private string saveSelectSceneName = "Save File Select Screen";

    /// <summary>
    /// Called when a boss button is clicked. Finds the scene for this boss and loads it.
    /// Wire each boss button's On Click () to BossSelectButton.OnClick (or call this directly with the boss name string).
    /// </summary>
    /// <param name="bossName">Display name of the boss (e.g. "Bubble Blum"). Must match an entry in bossDisplayNames.</param>
    public void OnBossSelected(string bossName)
    {
        if (string.IsNullOrEmpty(bossName))
        {
            Debug.LogWarning("BossSelectUI: boss name was empty.");
            return;
        }

        // Look up the scene name for this boss
        string sceneName = GetSceneNameForBoss(bossName);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("BossSelectUI: No scene found for boss '" + bossName + "'. Add it to the boss mapping.");
            return;
        }

        // Store the selected boss in GameManager so gameplay (e.g. boss defeat tracking) knows which boss we're fighting
        if (GameManager.Instance != null)
        {
            GameManager.Instance.selectedBoss = bossName;

            // Ensure combat style is set before entering the fight
            if (string.IsNullOrEmpty(GameManager.Instance.selectedCombatStyle))
                GameManager.Instance.SetDefaultCombatStyle();

            // Save the boss fight scene to the current save slot so Continue works later
            GameManager.Instance.SaveCurrentScene(sceneName);
            // Remember which scene we're in so Save File Select can save progress correctly
            GameManager.Instance.currentSceneName = sceneName;
        }

        Debug.Log("Loading boss fight: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Returns the scene name for the given boss, or null if not found.
    /// </summary>
    private string GetSceneNameForBoss(string bossName)
    {
        if (bossDisplayNames == null || bossSceneNames == null) return null;
        for (int i = 0; i < bossDisplayNames.Length && i < bossSceneNames.Length; i++)
        {
            if (bossDisplayNames[i] == bossName)
                return bossSceneNames[i];
        }
        return null;
    }

    /// <summary>
    /// Loads the Main Menu (Title Screen). Call from the "Back to Main Menu" button's On Click ().
    /// </summary>
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Loads the Save File Select screen. Call from the "Back to Save Select" button's On Click ().
    /// </summary>
    public void GoToSaveSelect()
    {
        SceneManager.LoadScene(saveSelectSceneName);
    }
}
