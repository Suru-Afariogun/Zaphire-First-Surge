using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Power bar UI. 6 image variants: index 0 = 0 power-ups, index 5 = 5 power-ups (max).
/// Each power-up is +15% attack from using a core. Subscribe to CoreManager power level.
/// </summary>
public class PowerBarUI : MonoBehaviour
{
    [Header("Power Bar Sprites")]
    [Tooltip("6 sprites: Index 0 = 0 power-ups, 1 = 1, ... 5 = 5 (max).")]
    public Sprite[] powerBarSprites = new Sprite[6];

    [Header("UI Reference")]
    public Image powerBarImage;

    private int lastPowerLevel = -1;

    private void Start()
    {
        if (powerBarImage == null)
            powerBarImage = GetComponent<Image>();
        if (powerBarImage == null)
            Debug.LogError("PowerBarUI: No Image component found!");
        if (powerBarSprites.Length != 6)
            Debug.LogWarning($"PowerBarUI: Expected 6 sprites (0-5 power level), found {powerBarSprites.Length}.");
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        if (powerBarImage == null) return;
        int level = CoreManager.Instance != null ? CoreManager.Instance.GetPowerLevel() : 0;
        if (level == lastPowerLevel) return;
        lastPowerLevel = level;

        int index = Mathf.Clamp(level, 0, 5);
        if (index < powerBarSprites.Length && powerBarSprites[index] != null)
            powerBarImage.sprite = powerBarSprites[index];
        else
            Debug.LogWarning($"PowerBarUI: No sprite for power level {level} (index {index}).");
    }
}
