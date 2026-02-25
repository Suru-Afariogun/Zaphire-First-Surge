using UnityEngine;

/// <summary>
/// Simple 2D camera follow for the player.
/// Attach this to the Main Camera and assign the player Transform.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The transform the camera should follow (e.g. the player).")]
    public Transform target;

    [Header("Offset")]
    [Tooltip("Offset from the target position. For 2D, usually (0, 0, -10).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Smoothing")]
    [Tooltip("How quickly the camera catches up to the target. 0 = instant.")]
    public float smoothTime = 0.15f;

    private Vector3 _velocity;

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
}

