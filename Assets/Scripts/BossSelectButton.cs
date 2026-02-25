using UnityEngine;

/// <summary>
/// Attach to each boss button. Set the boss name (e.g. "Bubble Blum"), then in the Button's
/// On Click () call this component's OnClick. The scene name is looked up in BossSelectUI.
/// </summary>
public class BossSelectButton : MonoBehaviour
{
    [Tooltip("Display name of this boss (e.g. 'Bubble Blum'). Must match an entry in BossSelectUI's boss mapping.")]
    [SerializeField] private string bossDisplayName;

    /// <summary>
    /// Call this from the Button's On Click () list. Loads this boss's fight scene.
    /// </summary>
    public void OnClick()
    {
        var ui = FindFirstObjectByType<BossSelectUI>();
        if (ui != null)
            ui.OnBossSelected(bossDisplayName);
        else
            Debug.LogWarning("BossSelectButton: BossSelectUI not found in scene.");
    }
}
