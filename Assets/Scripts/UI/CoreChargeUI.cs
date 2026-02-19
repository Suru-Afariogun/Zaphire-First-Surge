using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages a single core charge indicator. Switches between charged and discharged sprites.
/// You'll need 5 of these components (one for each core).
/// </summary>
public class CoreChargeUI : MonoBehaviour
{
    [Header("Core Sprites")]
    public Sprite chargedSprite;      // Sprite when core is charged
    public Sprite dischargedSprite;   // Sprite when core is discharged

    [Header("UI Reference")]
    public Image coreImage;  // The Image component that displays the core sprite

    [Header("Core Settings")]
    [Tooltip("Which core number is this? (0-4, where 0 is the leftmost core)")]
    public int coreIndex = 0;  // Which core this is (0-4)

    private bool isCharged = false;

    private void Start()
    {
        // Get Image component if not assigned
        if (coreImage == null)
        {
            coreImage = GetComponent<Image>();
            if (coreImage == null)
            {
                Debug.LogError($"CoreChargeUI (Core {coreIndex}): No Image component found! Add an Image component to this GameObject.");
            }
        }

        // Start discharged
        SetCharged(false);
    }

    /// <summary>
    /// Sets whether this core is charged or discharged.
    /// </summary>
    public void SetCharged(bool charged)
    {
        isCharged = charged;

        if (coreImage == null) return;

        // Switch sprite based on charge state
        if (charged && chargedSprite != null)
        {
            coreImage.sprite = chargedSprite;
        }
        else if (!charged && dischargedSprite != null)
        {
            coreImage.sprite = dischargedSprite;
        }
        else
        {
            Debug.LogWarning($"CoreChargeUI (Core {coreIndex}): Missing sprite! Charged: {charged}");
        }
    }

    /// <summary>
    /// Gets whether this core is currently charged.
    /// </summary>
    public bool IsCharged()
    {
        return isCharged;
    }
}
