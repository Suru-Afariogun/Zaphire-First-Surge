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
    private bool isShaking = false;
    private RectTransform _rect;
    private Vector3 originalLocalPos;
    private float shakeAmplitude = 5f;
    private float shakeFrequency = 25f;
    private bool originCaptured = false;

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

        // Capture original position after any layout groups have positioned the UI
        _rect = GetComponent<RectTransform>();
        if (_rect != null)
        {
            originalLocalPos = _rect.localPosition;
            originCaptured = true;
        }
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

    private void Update()
    {
        if (!originCaptured && _rect != null)
        {
            originalLocalPos = _rect.localPosition;
            originCaptured = true;
        }

        if (!originCaptured) return;

        if (isShaking)
        {
            float t = Time.unscaledTime * shakeFrequency;
            // Small jitter around original position
            Vector2 offset = new Vector2(Mathf.PerlinNoise(t, coreIndex) - 0.5f, Mathf.PerlinNoise(coreIndex, t) - 0.5f);
            _rect.localPosition = originalLocalPos + (Vector3)(offset * shakeAmplitude);
        }
        else
        {
            if (_rect.localPosition != originalLocalPos)
                _rect.localPosition = originalLocalPos;
        }
    }

    /// <summary>
    /// Enables or disables shaking for this core icon. Amplitude/frequency are adjustable.
    /// </summary>
    public void SetShaking(bool shaking, float amplitude, float frequency)
    {
        isShaking = shaking;
        shakeAmplitude = amplitude;
        shakeFrequency = frequency;
        if (!shaking)
        {
            if (_rect != null && originCaptured)
                _rect.localPosition = originalLocalPos;
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
