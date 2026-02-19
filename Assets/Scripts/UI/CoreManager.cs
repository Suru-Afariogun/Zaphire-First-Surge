using UnityEngine;

/// <summary>
/// Manages all 5 cores. Tracks which cores are charged and updates the UI.
/// Assign all 5 CoreChargeUI components in the Inspector.
/// </summary>
public class CoreManager : MonoBehaviour
{
    public static CoreManager Instance;

    [Header("Core UI References")]
    [Tooltip("Assign all 5 core UI components in order (left to right)")]
    public CoreChargeUI[] cores = new CoreChargeUI[5];

    [Header("Core Settings")]
    public int maxCores = 5;           // Maximum number of cores
    private int chargedCores = 0;       // Current number of charged cores

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
        // Validate setup
        if (cores.Length != 5)
        {
            Debug.LogWarning($"CoreManager: Expected 5 cores, but found {cores.Length}. Make sure all cores are assigned!");
        }

        // Initialize all cores as discharged
        ResetAllCores();
    }

    /// <summary>
    /// Charges one core (adds a charge).
    /// </summary>
    public void ChargeCore()
    {
        if (chargedCores >= maxCores)
        {
            Debug.Log("All cores already charged!");
            return;
        }

        // Charge the next available core
        if (chargedCores < cores.Length && cores[chargedCores] != null)
        {
            cores[chargedCores].SetCharged(true);
            chargedCores++;
            Debug.Log($"Core charged! Total charged: {chargedCores}/{maxCores}");
        }
    }

    /// <summary>
    /// Discharges one core (removes a charge).
    /// </summary>
    public void DischargeCore()
    {
        if (chargedCores <= 0)
        {
            Debug.Log("No cores charged!");
            return;
        }

        // Discharge the rightmost charged core
        chargedCores--;
        if (chargedCores >= 0 && chargedCores < cores.Length && cores[chargedCores] != null)
        {
            cores[chargedCores].SetCharged(false);
            Debug.Log($"Core discharged! Total charged: {chargedCores}/{maxCores}");
        }
    }

    /// <summary>
    /// Charges a specific number of cores.
    /// </summary>
    public void SetChargedCores(int count)
    {
        count = Mathf.Clamp(count, 0, maxCores);

        // Discharge all first
        ResetAllCores();

        // Charge up to the specified count
        for (int i = 0; i < count && i < cores.Length; i++)
        {
            if (cores[i] != null)
            {
                cores[i].SetCharged(true);
            }
        }

        chargedCores = count;
        Debug.Log($"Cores set to: {chargedCores}/{maxCores}");
    }

    /// <summary>
    /// Resets all cores to discharged state.
    /// </summary>
    public void ResetAllCores()
    {
        for (int i = 0; i < cores.Length; i++)
        {
            if (cores[i] != null)
            {
                cores[i].SetCharged(false);
            }
        }
        chargedCores = 0;
    }

    /// <summary>
    /// Gets the number of currently charged cores.
    /// </summary>
    public int GetChargedCores()
    {
        return chargedCores;
    }

    /// <summary>
    /// Gets whether all cores are charged.
    /// </summary>
    public bool AreAllCoresCharged()
    {
        return chargedCores >= maxCores;
    }
}
