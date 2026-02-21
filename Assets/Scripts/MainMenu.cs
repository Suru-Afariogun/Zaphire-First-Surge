using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Loads the save file select scene for starting a new game
    /// </summary>
    public void NewGame()
    {
        SceneManager.LoadScene("Save File Select Screen");
    }

    /// <summary>
    /// Continues from the last saved game. If no save is found, loads the save file select scene.
    /// </summary>
    public void ContinueGame()
    {
        // Prefer current selected save file if it has data
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.selectedSaveFile))
        {
            if (SaveFileSelectManager.Instance != null &&
                SaveFileSelectManager.Instance.TryContinueFromSaveFile(GameManager.Instance.selectedSaveFile))
            {
                return;
            }
        }

        // Otherwise try first available save, or open save file select
        if (SaveFileSelectManager.Instance != null && SaveFileSelectManager.Instance.TryContinueFromLatestSave())
            return;

        Debug.Log("No save file found. Loading Save File Select Screen.");
        if (SaveFileSelectManager.Instance != null)
            SaveFileSelectManager.Instance.LoadSaveFileSelectScene();
        else
            SceneManager.LoadScene("Save File Select Screen");
    }

    /// <summary>
    /// Opens the options menu
    /// </summary>
    public void OpenOptions()
    {
        SceneManager.LoadScene("Options");
    }

    /// <summary>
    /// Opens the save file select scene (for new game)
    /// </summary>
    public void OpenSaveFileSelect()
    {
        if (SaveFileSelectManager.Instance != null)
            SaveFileSelectManager.Instance.LoadSaveFileSelectScene();
        else
            SceneManager.LoadScene("Save File Select Screen");
    }

    // public void QuitGame() => Application.Quit();
}
