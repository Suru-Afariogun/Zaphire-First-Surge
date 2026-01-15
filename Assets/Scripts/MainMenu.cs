using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// Loads the save file select scene for starting a new game
    /// </summary>
    public void NewGame()
    {
        SceneManager.LoadScene("Save File Select");
    }

    /// <summary>
    /// Continues from the last saved game. If no save is found, loads the save file select scene.
    /// </summary>
    public void ContinueGame()
    {
        // Check if there's a selected save file with a saved scene
        if (!string.IsNullOrEmpty(GameManager.Instance.selectedSaveFile))
        {
            string savedScene = GameManager.Instance.GetSavedScene(GameManager.Instance.selectedSaveFile);
            if (!string.IsNullOrEmpty(savedScene))
            {
                Debug.Log("Loading saved scene: " + savedScene);
                SceneManager.LoadScene(savedScene);
                return;
            }
        }

        // If no save file is selected or no saved scene exists, check all save files
        // and use the most recent one if available
        string[] saveFiles = { "Save File 1", "Save File 2", "Save File 3" };
        string latestSaveFile = null;
        string latestScene = null;

        foreach (string saveFile in saveFiles)
        {
            string scene = GameManager.Instance.GetSavedScene(saveFile);
            if (!string.IsNullOrEmpty(scene))
            {
                latestSaveFile = saveFile;
                latestScene = scene;
                break; // Use first found save file
            }
        }

        if (!string.IsNullOrEmpty(latestScene))
        {
            GameManager.Instance.selectedSaveFile = latestSaveFile;
            Debug.Log("Loading saved scene: " + latestScene + " from " + latestSaveFile);
            SceneManager.LoadScene(latestScene);
        }
        else
        {
            // No save found, load save file select scene
            Debug.Log("No save file found. Loading Save File Select.");
            SceneManager.LoadScene("Save File Select");
        }
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
        SceneManager.LoadScene("Save File Select");
    }

    // public void QuitGame() => Application.Quit();
}
