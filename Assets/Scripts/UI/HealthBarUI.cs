using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Life bar UI. Uses 10 image variants: index 0 = 0 bars filled, index 5 = 5 bars, index 9 = full (10 bars).
/// When health is 50/100, shows image 5 (5 bars filled). Subscribe to PlayerHealth.OnHealthChanged.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Life Bar Sprites")]
    [Tooltip("10 sprites: Index 0 = 0 bars, 1 = 1 bar, ... 9 = 10 bars (full). Health 50 → image 5.")]
    public Sprite[] lifeBarSprites = new Sprite[10];

    [Header("UI Reference")]
    public Image lifeBarImage;

    private bool _subscribed;

    private void Start()
    {
        if (lifeBarImage == null)
            lifeBarImage = GetComponent<Image>();
        if (lifeBarImage == null)
            Debug.LogError("HealthBarUI: No Image component found!");

        if (lifeBarSprites.Length != 10)
            Debug.LogWarning($"HealthBarUI: Expected 10 life bar sprites (0-9 bars filled), found {lifeBarSprites.Length}.");
    }

    private void Update()
    {
        if (_subscribed) return;
        if (PlayerHealth.Instance == null) return;

        PlayerHealth.Instance.OnHealthChanged += UpdateLifeBar;
        UpdateLifeBar(PlayerHealth.Instance.currentHealth);
        _subscribed = true;
    }

    private void OnDestroy()
    {
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.OnHealthChanged -= UpdateLifeBar;
    }

    /// <summary>
    /// Picks sprite by bars filled: 0–10 bars → index 0–9 (index 9 = full).
    /// </summary>
    void UpdateLifeBar(int currentHealth)
    {
        if (lifeBarImage == null) return;

        int barsFilled = PlayerHealth.Instance != null ? PlayerHealth.Instance.GetBarsFilled() : 0;
        // Image index 0 = 0 bars, 5 = 5 bars, 9 = 10 bars (full). So index = min(9, barsFilled).
        int spriteIndex = Mathf.Min(9, barsFilled);

        if (spriteIndex < lifeBarSprites.Length && lifeBarSprites[spriteIndex] != null)
            lifeBarImage.sprite = lifeBarSprites[spriteIndex];
        else
            Debug.LogWarning($"HealthBarUI: No sprite for bars filled {barsFilled} (index {spriteIndex}).");
    }
}
