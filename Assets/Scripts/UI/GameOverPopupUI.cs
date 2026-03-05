using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Handles the Game Over popup. The prefab (no Canvas) is spawned by GameManager under the scene's "Game Over Canvas".
/// Shows lives (X/3) and boss health; if lives > 0, Continue respawns at PlayerSpawner; else Return to Boss Select. Game is paused while visible.
/// </summary>
public class GameOverPopupUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel to show/hide. Leave empty if this script is on the panel itself — we'll use CanvasGroup to hide so the script stays active.")]
    public GameObject rootPanel;

    [Tooltip("TextMeshPro element showing current lives (e.g., \"2/3\").")]
    public TMP_Text livesText;

    [Tooltip("TextMeshPro element showing how much life the boss had left when the player died (e.g., \"35/50\").")]
    public TMP_Text bossHealthText;

    [Tooltip("Continue button (enabled only when lives > 0).")]
    public Button continueButton;

    [Tooltip("Button that returns to the Boss Select screen.")]
    public Button returnToBossSelectButton;

    [Header("Dim Background")]
    [Tooltip("Optional full-screen Image behind the popup to darken the game while the popup is visible.")]
    public Image dimBackgroundImage;
    [Range(0f, 1f)]
    [Tooltip("Alpha to use for the dim background when the popup is visible.")]
    public float dimAlpha = 0.6f;

    [Header("Scene Names")]
    [Tooltip("Name of the Boss Select scene.")]
    public string bossSelectSceneName = "Boss Select Screen";

    private bool _subscribed;
    private PlayerSpawner _spawner;
    private BubbleBlumBoss _boss;
    private CanvasGroup _canvasGroupForHide;
    private bool _useCanvasGroupToHide;

    void Start()
    {
        // Hide on start. Either hide a child panel (rootPanel) or hide this object via CanvasGroup so this script stays active.
        if (rootPanel != null && rootPanel != gameObject)
        {
            rootPanel.SetActive(false);
        }
        else
        {
            _canvasGroupForHide = GetComponent<CanvasGroup>();
            if (_canvasGroupForHide == null)
                _canvasGroupForHide = gameObject.AddComponent<CanvasGroup>();
            _canvasGroupForHide.alpha = 0f;
            _canvasGroupForHide.blocksRaycasts = false;
            _canvasGroupForHide.interactable = false;
            _useCanvasGroupToHide = true;
        }

        _spawner = Object.FindFirstObjectByType<PlayerSpawner>();
        _boss = Object.FindFirstObjectByType<BubbleBlumBoss>();

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

        UpdateLivesUI();
        UpdateBossHealthUI();

        SetPopupVisible(true);

        // Select the appropriate button
        if (EventSystem.current != null)
        {
            if (continueButton != null && continueButton.interactable)
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
            else if (returnToBossSelectButton != null)
                EventSystem.current.SetSelectedGameObject(returnToBossSelectButton.gameObject);
        }
    }

    void SetPopupVisible(bool visible)
    {
        if (_useCanvasGroupToHide && _canvasGroupForHide != null)
        {
            _canvasGroupForHide.alpha = visible ? 1f : 0f;
            _canvasGroupForHide.blocksRaycasts = visible;
            _canvasGroupForHide.interactable = visible;
        }
        else if (rootPanel != null)
            rootPanel.SetActive(visible);

        if (dimBackgroundImage != null)
        {
            var c = dimBackgroundImage.color;
            c.a = visible ? dimAlpha : 0f;
            dimBackgroundImage.color = c;
            dimBackgroundImage.gameObject.SetActive(visible);
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
            _boss = Object.FindFirstObjectByType<BubbleBlumBoss>();
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

        SetPopupVisible(false);

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

        SetPopupVisible(false);

        // Reset lives for next attempts
        if (GameManager.Instance != null)
            GameManager.Instance.currentLives = GameManager.DefaultLives;

        if (!string.IsNullOrEmpty(bossSelectSceneName))
            ScreenFader.LoadScene(bossSelectSceneName);
    }
}

