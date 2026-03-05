using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Global screen fader used for scene transitions.
/// Place one instance in a startup scene (with a full-screen Image) and it will
/// persist across scenes. Use ScreenFader.LoadScene(\"SceneName\") instead of
/// SceneManager.LoadScene to get a fade-out / fade-in effect.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("Fade Settings")]
    [Tooltip("Full-screen Image used to fade to/from black. Should stretch to cover the whole screen.")]
    public Image fadeImage;

    [Tooltip("Seconds to fade out before loading the next scene.")]
    public float fadeOutDuration = 0.5f;

    [Tooltip("Seconds to fade back in after the next scene loads.")]
    public float fadeInDuration = 0.5f;

    private bool _isFading;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[ScreenFader] fadeImage is not assigned. Fades will be skipped.");
        }
    }

    /// <summary>
    /// Call this instead of SceneManager.LoadScene to perform a fade-out, load, then fade-in.
    /// Falls back to a direct load if no ScreenFader instance exists.
    /// </summary>
    public static void LoadScene(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        Instance.StartCoroutine(Instance.FadeAndSwitchScene(sceneName));
    }

    private IEnumerator FadeAndSwitchScene(string sceneName)
    {
        if (fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        if (_isFading)
            yield break;

        _isFading = true;

        // Fade out to black
        yield return Fade(0f, 1f, fadeOutDuration);

        SceneManager.LoadScene(sceneName);

        // Wait one frame so the new scene can render at least once
        yield return null;

        // Fade back in from black
        yield return Fade(1f, 0f, fadeInDuration);

        _isFading = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            Color c = fadeImage.color;
            c.a = to;
            fadeImage.color = c;
            yield break;
        }

        float t = 0f;
        Color color = fadeImage.color;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(t / duration);
            float alpha = Mathf.Lerp(from, to, normalized);
            color.a = alpha;
            fadeImage.color = color;
            yield return null;
        }

        color.a = to;
        fadeImage.color = color;
    }
}

