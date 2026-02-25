using UnityEngine;

/// <summary>
/// Manages core points (0–10) and power level (0–5).
/// 1 point per enemy hit; 2 points = 1 core charged. 5 cores = 10 points max.
/// Cores can be spent to: heal 2 bars (20 HP) or power up (+15% attack, max 5 times).
/// Assign the 5 CoreChargeUI components so they show filled when corePoints >= 2 per core.
/// </summary>
public class CoreManager : MonoBehaviour
{
    public static CoreManager Instance;

    [Header("Core UI References")]
    [Tooltip("Assign all 5 core UI components in order (left to right).")]
    public CoreChargeUI[] cores = new CoreChargeUI[5];

    [Header("Core Settings")]
    [Tooltip("Max core points. 2 points = 1 core; 5 cores = 10 points.")]
    public const int MaxCorePoints = 10;
    [Tooltip("HP restored when spending one core to heal (2 bars = 20 HP if maxHealth 100).")]
    public int healPerCore = 20;
    [Tooltip("Attack power increase per power-up (e.g. 0.15 = 15%).")]
    public float powerUpPercent = 0.15f;
    [Tooltip("Max number of power-ups (each consumes 1 core).")]
    public const int MaxPowerLevel = 5;

    private int corePoints = 0;
    private int powerLevel = 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        RefreshCoreUI();
    }

    /// <summary>
    /// Call when the player hits an enemy. Adds 1 point (max 10).
    /// </summary>
    public void AddCorePoint()
    {
        if (corePoints >= MaxCorePoints) return;
        corePoints++;
        RefreshCoreUI();
        Debug.Log($"Core point +1. Points: {corePoints}/{MaxCorePoints}");
    }

    /// <summary>
    /// Current core points (0–10). 2 points = one charged core.
    /// </summary>
    public int GetCorePoints()
    {
        return corePoints;
    }

    /// <summary>
    /// Number of charged cores (0–5). Each core = 2 points.
    /// </summary>
    public int GetChargedCores()
    {
        return corePoints / 2;
    }

    /// <summary>
    /// Whether the player has at least one charged core (2+ points).
    /// </summary>
    public bool HasChargedCore()
    {
        return corePoints >= 2;
    }

    /// <summary>
    /// Spend one core (2 points) to heal 2 bars. Returns true if spent.
    /// </summary>
    public bool UseCoreForHeal()
    {
        if (corePoints < 2) return false;
        corePoints -= 2;
        RefreshCoreUI();
        if (PlayerHealth.Instance != null)
            PlayerHealth.Instance.Heal(healPerCore);
        Debug.Log($"Used 1 core to heal {healPerCore} HP. Points left: {corePoints}");
        return true;
    }

    /// <summary>
    /// Spend one core (2 points) to power up (+15% attack, max 5). Returns true if spent.
    /// </summary>
    public bool UseCoreForPowerUp()
    {
        if (corePoints < 2 || powerLevel >= MaxPowerLevel) return false;
        corePoints -= 2;
        powerLevel++;
        RefreshCoreUI();
        Debug.Log($"Used 1 core to power up. Power level: {powerLevel}/{MaxPowerLevel}. Points left: {corePoints}");
        return true;
    }

    /// <summary>
    /// Current power level (0–5). Multiply base attack by (1 + powerLevel * powerUpPercent).
    /// </summary>
    public int GetPowerLevel()
    {
        return powerLevel;
    }

    /// <summary>
    /// Attack multiplier from power level (e.g. 1.0 at 0, 1.15 at 1, up to 1.75 at 5 if 15%).
    /// </summary>
    public float GetAttackMultiplier()
    {
        return 1f + powerLevel * powerUpPercent;
    }

    /// <summary>
    /// Updates the 5 core UIs: core i is charged if corePoints >= 2*(i+1) equivalent (points 2,4,6,8,10).
    /// Core 0 charged at 2+, core 1 at 4+, ... core 4 at 10.
    /// </summary>
    void RefreshCoreUI()
    {
        for (int i = 0; i < cores.Length && i < 5; i++)
        {
            if (cores[i] == null) continue;
            int pointsNeededForThisCore = (i + 1) * 2;
            cores[i].SetCharged(corePoints >= pointsNeededForThisCore);
        }
    }
}
