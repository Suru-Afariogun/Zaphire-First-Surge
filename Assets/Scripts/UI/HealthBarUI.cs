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
    private int _lastHealth = -1;
    private Color _originalColor;
    private Coroutine _healFlashCoroutine;
    private Coroutine _damageShakeCoroutine;
    private RectTransform _rect;
    private Vector3 _originalLocalPos;

    [Header("Heal Flash")]
    [Tooltip("Color to tint the life bar briefly when health increases (e.g., HealingCore).")]
    public Color healFlashColor = Color.green;
    [Tooltip("How long the heal flash lasts (seconds).")]
    public float healFlashDuration = 0.2f;

    [Header("Damage Shake")]
    [Tooltip("How far (in UI units) the life bar shakes when the player takes damage.")]
    public float damageShakeAmplitude = 5f;
    [Tooltip("How long the life bar shake lasts when the player takes damage.")]
    public float damageShakeDuration = 0.15f;

    private void Start()
    {
        if (lifeBarImage == null)
            lifeBarImage = GetComponent<Image>();
        if (lifeBarImage == null)
            Debug.LogError("HealthBarUI: No Image component found!");

        if (lifeBarSprites.Length != 10)
            Debug.LogWarning($"HealthBarUI: Expected 10 life bar sprites (0-9 bars filled), found {lifeBarSprites.Length}.");

        if (lifeBarImage != null)
            _originalColor = lifeBarImage.color;

        _rect = GetComponent<RectTransform>();
        if (_rect != null)
            _originalLocalPos = _rect.localPosition;
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

        bool isFirstUpdate = _lastHealth < 0;
        bool healed = !isFirstUpdate && currentHealth > _lastHealth;
        bool tookDamage = !isFirstUpdate && currentHealth < _lastHealth;
        _lastHealth = currentHealth;

        int barsFilled = PlayerHealth.Instance != null ? PlayerHealth.Instance.GetBarsFilled() : 0;
        // Image index 0 = 0 bars, 5 = 5 bars, 9 = 10 bars (full). So index = min(9, barsFilled).
        int spriteIndex = Mathf.Min(9, barsFilled);

        if (spriteIndex < lifeBarSprites.Length && lifeBarSprites[spriteIndex] != null)
            lifeBarImage.sprite = lifeBarSprites[spriteIndex];
        else
            Debug.LogWarning($"HealthBarUI: No sprite for bars filled {barsFilled} (index {spriteIndex}).");

        if (healed)
        {
            // Brief green flash to indicate healing took effect
            if (_healFlashCoroutine != null) StopCoroutine(_healFlashCoroutine);
            _healFlashCoroutine = StartCoroutine(HealFlashRoutine());
        }
        else if (tookDamage)
        {
            // Shake the life bar when taking damage
            if (_damageShakeCoroutine != null) StopCoroutine(_damageShakeCoroutine);
            _damageShakeCoroutine = StartCoroutine(DamageShakeRoutine());
        }
    }

    private System.Collections.IEnumerator HealFlashRoutine()
    {
        if (lifeBarImage == null) yield break;
        lifeBarImage.color = healFlashColor;
        yield return new WaitForSeconds(healFlashDuration);
        lifeBarImage.color = _originalColor;
        _healFlashCoroutine = null;
    }

    private System.Collections.IEnumerator DamageShakeRoutine()
    {
        if (_rect == null) yield break;
        float t = 0f;
        while (t < damageShakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = t / damageShakeDuration;
            float amp = damageShakeAmplitude * (1f - normalized);
            Vector2 offset = Random.insideUnitCircle * amp;
            _rect.localPosition = _originalLocalPos + (Vector3)offset;
            yield return null;
        }
        _rect.localPosition = _originalLocalPos;
        _damageShakeCoroutine = null;
    }
}
