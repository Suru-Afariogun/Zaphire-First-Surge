using UnityEngine;
using UnityEngine.SceneManagement;

public class BossSelectUI : MonoBehaviour
{
    public void OnBossSelected(string bossName)
    {
        // Store the selected boss name in GameManager
        GameManager.Instance.selectedBoss = bossName;
        Debug.Log("Selected Boss: " + bossName);

        // Ensure default combat style is set if not already selected
        if (string.IsNullOrEmpty(GameManager.Instance.selectedCombatStyle))
        {
            GameManager.Instance.SetDefaultCombatStyle();
        }

        // Save the current scene before loading the boss fight so continue-from-save works
        string bossFightScene = "BubbleBlumBossFight";
        GameManager.Instance.SaveCurrentScene(bossFightScene);
        // Remember which scene we're in so the Save File Select screen can save progress to the correct scene
        GameManager.Instance.currentSceneName = bossFightScene;

        // Load the boss fight scene (all bosses use same scene during early development)
        SceneManager.LoadScene(bossFightScene);
    }
}
