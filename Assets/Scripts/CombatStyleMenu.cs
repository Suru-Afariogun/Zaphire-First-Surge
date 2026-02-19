using UnityEngine;

/// <summary>
/// Combat Style Menu - Works like Mega Man 11's weapon select wheel.
/// Can be opened/closed with a button, and styles can be cycled with triggers.
/// </summary>
public class CombatStyleMenu : MonoBehaviour
{
    public static CombatStyleMenu Instance;
    
    [Header("UI References")]
    public GameObject combatStyleMenuUI;  // The menu panel/wheel UI
    public bool isMenuOpen = false;

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
        // Start with menu closed
        if (combatStyleMenuUI != null)
        {
            combatStyleMenuUI.SetActive(false);
            isMenuOpen = false;
        }
    }

    /// <summary>
    /// Toggles the combat style menu open/closed
    /// </summary>
    public void ToggleMenu()
    {
        if (combatStyleMenuUI == null) return;

        isMenuOpen = !isMenuOpen;
        combatStyleMenuUI.SetActive(isMenuOpen);

        // Optional: Pause time when menu is open (like Mega Man 11)
        // Time.timeScale = isMenuOpen ? 0f : 1f;
        
        Debug.Log("Combat Style Menu: " + (isMenuOpen ? "Opened" : "Closed"));
    }

    /// <summary>
    /// Opens the combat style menu
    /// </summary>
    public void OpenMenu()
    {
        if (combatStyleMenuUI == null) return;
        
        if (!isMenuOpen)
        {
            ToggleMenu();
        }
    }

    /// <summary>
    /// Closes the combat style menu
    /// </summary>
    public void CloseMenu()
    {
        if (combatStyleMenuUI == null) return;
        
        if (isMenuOpen)
        {
            ToggleMenu();
        }
    }
}
