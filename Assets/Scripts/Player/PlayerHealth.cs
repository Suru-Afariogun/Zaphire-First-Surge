using UnityEngine;

/// <summary>
/// Manages player health. Full health = 8 bars.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance;

    [Header("Health Settings")]
    public int maxHealth = 8;  // 8 bars total
    public int currentHealth;

    [Header("Events")]
    public System.Action<int> OnHealthChanged;  // Called when health changes (passes new health value)
    public System.Action OnDeath;              // Called when health reaches 0

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Start at full health
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }

    /// <summary>
    /// Reduces health by the specified amount.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth);

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Restores health by the specified amount.
    /// </summary>
    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return; // Already at full health

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

    /// <summary>
    /// Called when health reaches 0. Decrements GameManager lives so the save file shows the correct count when the player saves and continues later.
    /// </summary>
    void Die()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentLives > 0)
            GameManager.Instance.currentLives--;
        Debug.Log("Player died!");
        OnDeath?.Invoke();
        // You can add death logic here (respawn, game over screen, etc.)
    }

    /// <summary>
    /// Gets current health as a percentage (0.0 to 1.0).
    /// </summary>
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}
