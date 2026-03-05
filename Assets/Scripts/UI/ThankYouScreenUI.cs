using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Controls the standalone "Thank You For Playing" scene.
/// Shows buttons that can open external links (Patreon, Instagram, LinkedIn)
/// and a button to return to another scene (e.g., Boss Select or Title).
/// </summary>
public class ThankYouScreenUI : MonoBehaviour
{
    [Header("External Links")]
    [Tooltip("Full URL to your Patreon page (e.g., https://www.patreon.com/yourname).")]
    public string patreonUrl;

    [Tooltip("Full URL to your Instagram page (e.g., https://www.instagram.com/yourname).")]
    public string instagramUrl;

    [Tooltip("Full URL to your LinkedIn page (e.g., https://www.linkedin.com/in/yourname).")]
    public string linkedInUrl;

    [Header("Return Navigation")]
    [Tooltip("Scene to load when the Return/Back button is pressed (e.g., Boss Select Screen or Title Screen).")]
    public string returnSceneName = "Boss Select Screen";

    [Header("Initial Selection")]
    [Tooltip("Button that should be selected/focused first when the scene loads.")]
    public Button defaultSelectedButton;

    private void Start()
    {
        // Make sure a button is selected so controller/keyboard navigation works immediately.
        if (EventSystem.current != null && defaultSelectedButton != null)
        {
            EventSystem.current.SetSelectedGameObject(defaultSelectedButton.gameObject);
        }
    }

    public void OnPatreonClicked()
    {
        OpenUrlIfValid(patreonUrl, "Patreon");
    }

    public void OnInstagramClicked()
    {
        OpenUrlIfValid(instagramUrl, "Instagram");
    }

    public void OnLinkedInClicked()
    {
        OpenUrlIfValid(linkedInUrl, "LinkedIn");
    }

    public void OnReturnClicked()
    {
        if (string.IsNullOrEmpty(returnSceneName))
        {
            Debug.LogWarning("[ThankYouScreenUI] returnSceneName is empty; assign a scene name in the Inspector.");
            return;
        }

        ScreenFader.LoadScene(returnSceneName);
    }

    private void OpenUrlIfValid(string url, string label)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[ThankYouScreenUI] " + label + " URL is empty. Assign it in the Inspector.");
            return;
        }

        Application.OpenURL(url);
    }
}

