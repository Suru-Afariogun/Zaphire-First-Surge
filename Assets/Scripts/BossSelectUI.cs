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

        // Save the current scene before loading the boss fight
        // This allows the player to continue from the boss fight later
        string bossFightScene = "BubbleBlumBossFight";
        GameManager.Instance.SaveCurrentScene(bossFightScene);

        // Load the boss fight scene directly
        // For now, all bosses load the same scene during early development
        SceneManager.LoadScene(bossFightScene);
    }
}
