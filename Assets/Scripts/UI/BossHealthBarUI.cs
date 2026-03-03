using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss life bar UI for Bubble Blum.
/// Uses 23 image variants: index 0 = 0 health (empty bar), index 22 = full health.
/// Maps the boss's current health percentage to 0–22 and picks the corresponding sprite.
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Life Bar Sprites")]
    [Tooltip("23 sprites: Index 0 = 0 health, 1 = low, ... 22 = full health.")]
    public Sprite[] lifeBarSprites = new Sprite[23];

    [Header("UI Reference")]
    public Image lifeBarImage;

    [Header("Boss Reference")]
    [Tooltip("Optional explicit reference to the boss. If left empty, the script will find BubbleBlumBoss in the scene.")]
    public BubbleBlumBoss boss;

    private const int HealthLevels = 22; // 0..22 inclusive (0 = empty, 22 = full)
    private int _lastHealth = -1;
    private Color _originalColor;
    private Coroutine _damageFlashCoroutine;
    private Coroutine _damageShakeCoroutine;
    private RectTransform _rect;
    private Vector3 _originalLocalPos;

    [Header("Damage Feedback")]
    [Tooltip("Color to tint the boss life bar briefly when the boss takes damage.")]
    public Color damageFlashColor = Color.red;
    [Tooltip("How long the damage flash lasts (seconds).")]
    public float damageFlashDuration = 0.12f;
    [Tooltip("How far (in UI units) the boss life bar shakes when the boss takes damage.")]
    public float damageShakeAmplitude = 5f;
    [Tooltip("How long the boss life bar shake lasts when the boss takes damage.")]
    public float damageShakeDuration = 0.15f;

    private void Start()
    {
        if (lifeBarImage == null)
            lifeBarImage = GetComponent<Image>();
        if (lifeBarImage == null)
            Debug.LogError("BossHealthBarUI: No Image component found!");

        if (lifeBarSprites.Length != 23)
            Debug.LogWarning($"BossHealthBarUI: Expected 23 life bar sprites (0-22), found {lifeBarSprites.Length}.");

        if (lifeBarImage != null)
            _originalColor = lifeBarImage.color;
        _rect = GetComponent<RectTransform>();
        if (_rect != null)
            _originalLocalPos = _rect.localPosition;
    }

    private void Update()
    {
        if (lifeBarImage == null) return;

        if (boss == null)
            boss = FindObjectOfType<BubbleBlumBoss>();
        if (boss == null) return;

        int current = boss.CurrentHealth;
        int max = boss.MaxHealth;
        if (max <= 0) return;

        bool isFirstUpdate = _lastHealth < 0;
        bool tookDamage = !isFirstUpdate && current < _lastHealth;
        _lastHealth = current;

        // Map health percentage to 0..HealthLevels, where 0 = empty, HealthLevels = full
        float pct = Mathf.Clamp01((float)current / max);
        int barsFilled = Mathf.RoundToInt(pct * HealthLevels);
        barsFilled = Mathf.Clamp(barsFilled, 0, HealthLevels);

        int spriteIndex = Mathf.Clamp(barsFilled, 0, lifeBarSprites.Length - 1);

        if (lifeBarSprites[spriteIndex] != null)
            lifeBarImage.sprite = lifeBarSprites[spriteIndex];
        else
            Debug.LogWarning($"BossHealthBarUI: No sprite for health level {barsFilled} (index {spriteIndex}).");

        if (tookDamage)
        {
            if (_damageFlashCoroutine != null) StopCoroutine(_damageFlashCoroutine);
            if (_damageShakeCoroutine != null) StopCoroutine(_damageShakeCoroutine);
            _damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
            _damageShakeCoroutine = StartCoroutine(DamageShakeRoutine());
        }
    }

    private System.Collections.IEnumerator DamageFlashRoutine()
    {
        if (lifeBarImage == null) yield break;
        lifeBarImage.color = damageFlashColor;
        yield return new WaitForSeconds(damageFlashDuration);
        lifeBarImage.color = _originalColor;
        _damageFlashCoroutine = null;
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

