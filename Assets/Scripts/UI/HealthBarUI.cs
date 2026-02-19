using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the health bar UI. Displays the correct PNG based on health (0-8 bars).
/// Assign the 9 health bar sprites in order: 0 bars, 1 bar, 2 bars... 8 bars.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Health Bar Sprites")]
    [Tooltip("Array of health bar sprites. Index 0 = 0 bars, Index 1 = 1 bar, ... Index 8 = 8 bars (full)")]
    public Sprite[] healthBarSprites = new Sprite[9];  // 0-8 bars = 9 sprites total

    [Header("UI Reference")]
    public Image healthBarImage;  // The Image component that displays the health bar sprite

    private void Start()
    {
        // Subscribe to health changes
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged += UpdateHealthBar;
            // Set initial health bar
            UpdateHealthBar(PlayerHealth.Instance.currentHealth);
        }
        else
        {
            Debug.LogWarning("HealthBarUI: PlayerHealth.Instance not found! Make sure PlayerHealth component exists.");
        }

        // Validate setup
        if (healthBarImage == null)
        {
            healthBarImage = GetComponent<Image>();
            if (healthBarImage == null)
            {
                Debug.LogError("HealthBarUI: No Image component found! Add an Image component to this GameObject.");
            }
        }

        if (healthBarSprites.Length != 9)
        {
            Debug.LogWarning($"HealthBarUI: Expected 9 health bar sprites (0-8 bars), but found {healthBarSprites.Length}. Make sure all sprites are assigned!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.OnHealthChanged -= UpdateHealthBar;
        }
    }

    /// <summary>
    /// Updates the health bar sprite based on current health.
    /// </summary>
    void UpdateHealthBar(int currentHealth)
    {
        if (healthBarImage == null) return;

        // Clamp health to valid range (0-8)
        int healthIndex = Mathf.Clamp(currentHealth, 0, 8);

        // Make sure we have a sprite for this health level
        if (healthIndex < healthBarSprites.Length && healthBarSprites[healthIndex] != null)
        {
            healthBarImage.sprite = healthBarSprites[healthIndex];
        }
        else
        {
            Debug.LogWarning($"HealthBarUI: No sprite assigned for health level {healthIndex}!");
        }
    }
}
