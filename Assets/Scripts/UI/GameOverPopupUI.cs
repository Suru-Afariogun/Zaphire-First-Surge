using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles the Game Over popup:
/// - Shows "Game oVer, will you like to continue." and current lives (X/3)
/// - If lives > 0, player can Continue: player respawns at the PlayerSpawner, boss health is preserved
/// - If lives == 0, Continue is disabled; only Return to Boss Select is allowed
/// While the popup is visible, the game is paused (Time.timeScale = 0) until the player decides.
/// </summary>
public class GameOverPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Root panel of the Game Over popup.")]
    public GameObject rootPanel;

    [Tooltip("TextMeshPro element showing current lives (e.g., \"2/3\").")]
    public TMP_Text livesText;

    [Tooltip("TextMeshPro element showing how much life the boss had left when the player died (e.g., \"35/50\").")]
    public TMP_Text bossHealthText;

    [Tooltip("Continue button (enabled only when lives > 0).")]
    public Button continueButton;

    [Tooltip("Button that returns to the Boss Select screen.")]
    public Button returnToBossSelectButton;

    [Header("Scene Names")]
    [Tooltip("Name of the Boss Select scene.")]
    public string bossSelectSceneName = "Boss Select Screen";

    private bool _subscribed;
    private PlayerSpawner _spawner;
    private BubbleBlumBoss _boss;

    void Start()
    {
        // Only hide the root panel if it's not the same GameObject this script is on.
        // If we disable our own GameObject, Update will never run and the death event will never be subscribed.
        if (rootPanel != null && rootPanel != gameObject)
            rootPanel.SetActive(false);

        _spawner = FindObjectOfType<PlayerSpawner>();
        _boss = FindObjectOfType<BubbleBlumBoss>();

        // Wire buttons
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueClicked);
        if (returnToBossSelectButton != null)
            returnToBossSelectButton.onClick.AddListener(OnReturnToBossSelectClicked);
    }

    void Update()
    {
        if (_subscribed) return;
        if (PlayerHealth.Instance == null) return;

        PlayerHealth.Instance.OnDeath += OnPlayerDeath;
        _subscribed = true;
    }

    void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnDeath -= OnPlayerDeath;
    }

    void OnPlayerDeath()
    {
        // Hide or disable the player while they decide
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.gameObject.SetActive(false);

        // Pause the game while the popup is active
        Time.timeScale = 0f;
        PauseMenu.isPaused = true;

        UpdateLivesUI();
        UpdateBossHealthUI();

        if (rootPanel != null)
            rootPanel.SetActive(true);

        // Select the appropriate button
        if (EventSystem.current != null)
        {
            if (continueButton != null && continueButton.interactable)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            else if (returnToBossSelectButton != null)
                EventSystem.current.SetSelectedGameObject(returnToBossSelectButton.gameObject);
        }
    }

    void UpdateLivesUI()
    {
        int lives = GameManager.Instance != null ? GameManager.Instance.currentLives : 0;
        int maxLives = GameManager.DefaultLives;

        if (livesText != null)
            livesText.text = $"{lives}/{maxLives}";

        if (continueButton != null)
            continueButton.interactable = lives > 0;
    }

    void UpdateBossHealthUI()
    {
        if (bossHealthText == null) return;

        if (_boss == null)
            _boss = FindObjectOfType<BubbleBlumBoss>();
        if (_boss == null)
        {
            bossHealthText.text = "";
            return;
        }

        int current = _boss.CurrentHealth;
        int max = _boss.MaxHealth;
        bossHealthText.text = $"{current}/{max}";
    }

    void OnContinueClicked()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.currentLives <= 0) return; // safety

        // Unpause
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        // Respawn player at the spawner's position, preserving boss state
        RespawnPlayer();
    }

    void RespawnPlayer()
    {
        if (_spawner == null)
            _spawner = FindObjectOfType<PlayerSpawner>();

        Vector3 spawnPos = Vector3.zero;
        if (_spawner != null && _spawner.spawnPoint != null)
            spawnPos = _spawner.spawnPoint.position;
        else if (PlayerHealth.Instance != null)
            spawnPos = PlayerHealth.Instance.transform.position; // fallback

        if (PlayerHealth.Instance != null)
        {
            var playerGO = PlayerHealth.Instance.gameObject;
            playerGO.transform.position = spawnPos;
            PlayerHealth.Instance.RestoreFullHealth();
            playerGO.SetActive(true);
        }
    }

    void OnReturnToBossSelectClicked()
    {
        // Unpause
        Time.timeScale = 1f;
        PauseMenu.isPaused = false;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        // Reset lives for next attempts
        if (GameManager.Instance != null)
            GameManager.Instance.currentLives = GameManager.DefaultLives;

        if (!string.IsNullOrEmpty(bossSelectSceneName))
            SceneManager.LoadScene(bossSelectSceneName);
    }
}

