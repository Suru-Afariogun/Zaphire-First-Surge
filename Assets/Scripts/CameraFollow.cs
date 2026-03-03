using UnityEngine;

/// <summary>
/// Simple 2D camera follow for the player.
/// Attach this to the Main Camera and assign the player Transform.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;
    [Header("Target")]
    [Tooltip("The transform the camera should follow (e.g. the player).")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset from the target position. For 2D, usually (0, 0, -10).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up to the target. 0 = instant.")]
    public float smoothTime = 0.15f;

    [Header("Screen Shake")]
    [Tooltip("Default duration (seconds) for a small camera shake.")]
    public float smallShakeDuration = 0.15f;
    [Tooltip("Default amplitude for a small camera shake (position offset magnitude).")]
    public float smallShakeAmplitude = 0.1f;

    private Vector3 _velocity;
    private float _shakeTimeRemaining = 0f;
    private float _shakeTotalDuration = 0f;
    private float _shakeAmplitude = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        TryFindTarget();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            TryFindTarget();
            if (target == null) return;
        }

        // Desired position based on target + offset
        Vector3 desiredPosition = target.position + offset;

        // Apply screen shake offset if active
        if (_shakeTimeRemaining > 0f)
        {
            _shakeTimeRemaining -= Time.deltaTime;
            float t = _shakeTotalDuration > 0f ? (_shakeTimeRemaining / _shakeTotalDuration) : 0f;
            float currentAmp = _shakeAmplitude * t;
            _shakeOffset = Random.insideUnitSphere * currentAmp;
            _shakeOffset.z = 0f; // keep shake in XY plane for 2D
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }

        desiredPosition += _shakeOffset;

        if (smoothTime <= 0f)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                smoothTime
            );
        }
    }

    /// <summary>
    /// Automatically finds the player instance spawned by GameManager/PlayerSpawner.
    /// Prefers PlayerHealth.Instance (whichever prefab was chosen), falls back to tag \"Player\".
    /// </summary>
    void TryFindTarget()
    {
        if (target != null) return;

        // Use the current PlayerHealth instance if it exists (this is the chosen player prefab).
        if (PlayerHealth.Instance != null)
        {
            target = PlayerHealth.Instance.transform;
            return;
        }

        // Fallback: try finding an object tagged \"Player\"
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
            target = p.transform;
    }

    /// <summary>
    /// Starts a small default screen shake (used for core start/end feedback).
    /// </summary>
    public void ShakeSmall()
    {
        StartShake(smallShakeAmplitude, smallShakeDuration);
    }

    /// <summary>
    /// Starts a shake with custom amplitude and duration.
    /// </summary>
    public void StartShake(float amplitude, float duration)
    {
        _shakeAmplitude = Mathf.Max(_shakeAmplitude, Mathf.Abs(amplitude));
        _shakeTotalDuration = Mathf.Max(_shakeTotalDuration, duration);
        _shakeTimeRemaining = Mathf.Max(_shakeTimeRemaining, duration);
    }
}


