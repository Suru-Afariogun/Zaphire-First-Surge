using System.Collections;
using UnityEngine;

/// <summary>
/// Manages player health. Total health = 100 (10 bars of 10 HP each).
/// Life bar UI uses 10 image variants: image index = bars filled (0–9; 10th image = full).
/// On take damage: white sprite flicker and optional knockback (adjustable for upgrades).
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    [Tooltip("Total health. Life bar shows 10 'bars'; each bar = maxHealth/10.")]
    public int maxHealth = 100;
    [Tooltip("Current health (0 to maxHealth).")]
    public int currentHealth;

    /// <summary>Number of "bars" (each bar = maxHealth/10). Used by life bar UI.</summary>
    public const int HealthBarsCount = 10;

    [Header("Damage Flicker (when taking damage)")]
    [Tooltip("How long the damage flicker lasts.")]
    public float damageFlickerDuration = 0.12f;
    [Tooltip("Color to tint the sprite when taking damage.")]
    public Color damageFlickerColor = Color.red;

    [Header("HealingCore Flicker")]
    [Tooltip("How long the flicker lasts when HealingCore activates.")]
    public float healingCoreFlickerDuration = 0.12f;
    [Tooltip("Color to tint the sprite when HealingCore activates.")]
    public Color healingCoreFlickerColor = Color.green;

    [Header("PowerCore Flicker")]
    [Tooltip("How long the flicker lasts when PowerCore activates.")]
    public float powerCoreFlickerDuration = 0.12f;
    [Tooltip("Color to tint the sprite when PowerCore activates.")]
    public Color powerCoreFlickerColor = Color.cyan;
    [Tooltip("Default knockback force when not specified by caller. Upgrades can change this.")]
    public float defaultKnockbackForce = 12f;
    [Tooltip("Optional. Assign if not on same GameObject.")]
    public Rigidbody2D rb;
    [Tooltip("Optional. Assign if not on same GameObject.")]
    public SpriteRenderer spriteRenderer;

    [Header("Events")]
    public System.Action<int> OnHealthChanged;  // Called when health changes (passes new health value)
    public System.Action OnDeath;              // Called when health reaches 0

    private Color _originalColor;
    private Coroutine _flickerCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _originalColor = spriteRenderer.color;
    }

    /// <summary>Returns how many bars are filled (0–10). Used by life bar to pick the correct image.</summary>
    public int GetBarsFilled()
    {
        if (maxHealth <= 0) return 0;
        // 0–100 health → 0–10 bars; clamp so we never return > 10
        int bars = Mathf.RoundToInt((float)currentHealth / maxHealth * HealthBarsCount);
        return Mathf.Clamp(bars, 0, HealthBarsCount);
    }

    /// <summary>
    /// Reduces health by the specified amount. No knockback or flicker (e.g. pits, hazards).
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, default, 0f);
    }

    /// <summary>
    /// Reduces health and applies white-hit flicker. If knockbackForce > 0 and hitFromPosition is set, applies knockback away from hit source (upgradeable via defaultKnockbackForce or per-call).
    /// </summary>
    public void TakeDamage(int damage, Vector2 hitFromPosition, float knockbackForce)
    {
        Debug.Log($"[PlayerHealth] TakeDamage called. damage={damage}, hitFrom={hitFromPosition}, knockbackForce={knockbackForce}");
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);

        PlayDamageFlicker();

        // Knockback away from hit source
        float force = knockbackForce > 0f ? knockbackForce : defaultKnockbackForce;
        if (force > 0f && rb != null && hitFromPosition != default)
        {
            Vector2 dir = ((Vector2)transform.position - hitFromPosition).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.left; // fallback
            rb.linearVelocity = dir * force;
        }

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    /// <summary>Damage flicker (used when taking damage).</summary>
    public void PlayDamageFlicker() => PlayFlicker(damageFlickerColor, damageFlickerDuration);

    /// <summary>HealingCore activation flicker.</summary>
    public void PlayHealingCoreFlicker() => PlayFlicker(healingCoreFlickerColor, healingCoreFlickerDuration);

    /// <summary>PowerCore activation flicker.</summary>
    public void PlayPowerCoreFlicker() => PlayFlicker(powerCoreFlickerColor, powerCoreFlickerDuration);

    void PlayFlicker(Color color, float duration)
    {
        if (spriteRenderer == null) return;
        if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
        _flickerCoroutine = StartCoroutine(FlickerTint(color, duration));
    }

    private IEnumerator FlickerTint(Color color, float duration)
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = _originalColor;
        _flickerCoroutine = null;
    }

    /// <summary>
    /// Restores health by the specified amount (e.g. 20 for 2 bars when using a core).
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Sets health to a specific value.
    /// </summary>
    public void SetHealth(int health)
    {
        currentHealth = Mathf.Clamp(health, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Fully restores health.
    /// </summary>
    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
        Debug.Log("Player health fully restored!");
    }

    void Die()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentLives > 0)
            GameManager.Instance.currentLives--;
        Debug.Log("Player died!");
        OnDeath?.Invoke();
    }

    /// <summary>
    /// Gets current health as a percentage (0.0 to 1.0).
    /// </summary>
    public float GetHealthPercentage()
    {
        return maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    }
}
