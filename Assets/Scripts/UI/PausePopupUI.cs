using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Pause menu popup that shows the controls.
/// Attach this to your Pause popup panel (the same object referenced by PauseMenu.pauseMenuUI),
/// assign the controlsText field to a TextMeshProUGUI, and it will auto-generate a controls table
/// similar to the README. You can edit the rows in the Inspector to change the in-game text.
/// </summary>
public class PausePopupUI : MonoBehaviour
{
    public static PausePopupUI Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("UI References")]
    [Tooltip("Panel to show/hide while paused (e.g., the child with the controls text and buttons).")]
    public GameObject rootPanel;

    [Tooltip("TextMeshPro text that will display the controls table.")]
    public TMP_Text controlsText;

    [Tooltip("Optional full-screen Image behind the popup to darken the game while paused.")]
    public Image dimBackgroundImage;

    [Range(0f, 1f)]
    [Tooltip("Alpha to use for the dim background when the pause popup is visible.")]
    public float dimAlpha = 0.6f;

    [System.Serializable]
    public class ControlEntry
    {
        public string action;
        public string keyboard;
        public string gamepad;
    }

    [Header("Controls (editable)")]
    [Tooltip("Rows for the controls table. Edit these to change what appears in the pause menu.")]
    public ControlEntry[] controls;

    private CanvasGroup _canvasGroupForHide;
    private bool _useCanvasGroupToHide;

    private void Awake()
    {
        // Simple singleton-style access for toggling from input code
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // If no rows were set up in the Inspector, populate with sensible defaults
        if (controls == null || controls.Length == 0)
        {
            controls = new[]
            {
                new ControlEntry { action = "Move",              keyboard = "W A S D / Arrow keys",       gamepad = "Left stick"          },
                new ControlEntry { action = "Jump",              keyboard = "Space / U",                  gamepad = "A (South)"           },
                new ControlEntry { action = "Light Attack",      keyboard = "M / I",                      gamepad = "X (West)"            },
                new ControlEntry { action = "Heavy Attack",      keyboard = "B / O",                      gamepad = "Y (North)"           },
                new ControlEntry { action = "Dash",              keyboard = "N / P",                      gamepad = "B (East)"            },
                new ControlEntry { action = "Healing Core",      keyboard = "Q / V (hold to charge)",     gamepad = "LB (Left shoulder)"  },
                new ControlEntry { action = "Power Core",        keyboard = "E / C (hold to charge)",     gamepad = "RB (Right shoulder)" },
                new ControlEntry { action = "Combat Style Menu", keyboard = "Tab",                        gamepad = "Start"               },
                new ControlEntry { action = "Combat Style Left", keyboard = "Z",                          gamepad = "LT (Left trigger)"   },
                new ControlEntry { action = "Combat Style Right",keyboard = "X",                          gamepad = "RT (Right trigger)"  },
            };
        }
    }

    private void OnEnable()
    {
        BuildControlsText();
    }

    private void Start()
    {
        // Match GameOverPopupUI pattern: hide a child panel or use CanvasGroup on this object.
        if (rootPanel != null && rootPanel != gameObject)
        {
            rootPanel.SetActive(false);
        }
        else
        {
            _canvasGroupForHide = GetComponent<CanvasGroup>();
            if (_canvasGroupForHide == null)
                _canvasGroupForHide = gameObject.AddComponent<CanvasGroup>();
            _canvasGroupForHide.alpha = 0f;
            _canvasGroupForHide.blocksRaycasts = false;
            _canvasGroupForHide.interactable = false;
            _useCanvasGroupToHide = true;
        }

        SetPopupVisible(false);
        IsPaused = false;
    }

    /// <summary>
    /// Toggles the pause popup on/off and manages Time.timeScale.
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeFromPause();
        }
        else
        {
            EnterPause();
        }
    }

    private void EnterPause()
    {
        if (IsPaused) return;
        IsPaused = true;
        SetPopupVisible(true);
        Time.timeScale = 0f;
    }

    private void ResumeFromPause()
    {
        if (!IsPaused) return;
        IsPaused = false;
        SetPopupVisible(false);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Builds a simple table-like text of controls using the same layout as the README.
    /// </summary>
    public void BuildControlsText()
    {
        if (controlsText == null || controls == null) return;

        var sb = new StringBuilder();

        // Header (bold to stand out)
        sb.AppendLine("<b>Action | Keyboard | Gamepad</b>");
        sb.AppendLine("--------------------------------------");

        foreach (var entry in controls)
        {
            if (entry == null) continue;
            sb.AppendLine($"{entry.action} | {entry.keyboard} | {entry.gamepad}");
        }

        controlsText.text = sb.ToString();
    }

    private void SetPopupVisible(bool visible)
    {
        if (_useCanvasGroupToHide && _canvasGroupForHide != null)
        {
            _canvasGroupForHide.alpha = visible ? 1f : 0f;
            _canvasGroupForHide.blocksRaycasts = visible;
            _canvasGroupForHide.interactable = visible;
        }
        else if (rootPanel != null)
        {
            rootPanel.SetActive(visible);
        }

        if (dimBackgroundImage != null)
        {
            Color c = dimBackgroundImage.color;
            c.a = visible ? dimAlpha : 0f;
            dimBackgroundImage.color = c;
            dimBackgroundImage.gameObject.SetActive(visible);
        }
    }
}

