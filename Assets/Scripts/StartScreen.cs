using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    /// <summary>
    /// Loads the Start Menu Screen
    /// </summary>
    public void NewGame()
    {
        ScreenFader.LoadScene("Start Menu screen");
    }

   
    // public void QuitGame() => Application.Quit();
}
