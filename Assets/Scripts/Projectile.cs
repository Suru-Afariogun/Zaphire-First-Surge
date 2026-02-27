using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable projectile for player and bosses. One script for many prefabs.
/// - Adjustable damage, travel distance/time, and max of this type on screen.
/// - Three animation states: firedProjectile, airborneProjectile, impactedProjectile.
/// - Dissipates if it doesn't hit anything before max time (or max distance).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Damage & Owner")]
    [Tooltip("Damage dealt when this projectile hits a valid target.")]
    public int damage = 10;
    [Tooltip("If true, damages enemies (e.g. boss). If false, damages the player.")]
    public bool isPlayerProjectile = true;
    [Tooltip("Set by spawner so this projectile ignores the shooter (e.g. boss projectile won't hit the boss).")]
    public Transform owner;
    [Tooltip("Knockback force applied when this projectile hits the player. Ignored for enemy targets.")]
    public float knockbackForceOnPlayer = 12f;
    [Tooltip("Knockback force applied when this projectile hits an enemy (e.g. boss).")]
    public float knockbackForceOnEnemy = 6f;

    [Header("Movement")]
    [Tooltip("Speed in units per second.")]
    public float speed = 15f;
    [Tooltip("Direction is set by the spawner via SetDirection or Initialize. Defaults to right.")]
    public Vector2 moveDirection = Vector2.right;
    [Tooltip("Max time in seconds before dissipating if nothing is hit. 0 = no time limit (use max distance).")]
    public float maxLifetime = 3f;
    [Tooltip("Max distance in units before dissipating. 0 = no distance limit (use max lifetime).")]
    public float maxDistance = 0f;

    [Header("On-Screen Limit (per type)")]
    [Tooltip("Unique id for this projectile type (e.g. \"PlayerBullet\", \"BossBubble\"). Used to cap how many can exist at once.")]
    public string projectileTypeId = "Default";
    [Tooltip("Max number of this type allowed on screen. Player might have 3, boss 6; upgradeable later.")]
    public int maxOnScreen = 3;

    [Header("Animation")]
    public Animator animator;
    [Tooltip("Animator trigger for fired state (played once at spawn).")]
    public string triggerFiredProjectile = "FiredProjectile";
    [Tooltip("Animator trigger or state for airborne (optional; can rely on transition from Fired).")]
    public string triggerAirborneProjectile = "AirborneProjectile";
    [Tooltip("Animator trigger when hitting something or dissipating.")]
    public string triggerImpactedProjectile = "ImpactedProjectile";
    [Tooltip("Seconds to wait after playing impact animation before destroying.")]
    public float impactDestroyDelay = 0.15f;

    private Rigidbody2D _rb;
    private float _spawnTime;
    private float _distanceTraveled;
    private bool _impacted;
    private static Dictionary<string, int> s_activeCountByType = new Dictionary<string, int>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb != null)
            _rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        if (!TryRegister(projectileTypeId, maxOnScreen))
        {
            Destroy(gameObject);
            return;
        }

        _spawnTime = Time.time;
        _distanceTraveled = 0f;
        _impacted = false;
        moveDirection = moveDirection.normalized;

        if (animator != null)
        {
            SetAnimatorTriggerSafe(triggerFiredProjectile);
            SetAnimatorTriggerSafe(triggerAirborneProjectile);
        }
    }

    void SetAnimatorTriggerSafe(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;
        for (int i = 0; i < animator.parameterCount; i++)
        {
            var p = animator.GetParameter(i);
            if (p.name == triggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(triggerName);
                return;
            }
        }
    }

    private void OnDisable()
    {
        Unregister(projectileTypeId);
    }

    /// <summary>Call after instantiate to set direction (and optionally override damage/speed). Projectile will face the same direction it is moving.</summary>
    public void SetDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude > 0.01f)
            moveDirection = direction.normalized;

        // Face the same direction as the shooter (projectile faces its movement direction)
        Vector3 s = transform.localScale;
        float faceX = moveDirection.x >= 0.01f ? 1f : (moveDirection.x <= -0.01f ? -1f : 1f);
        if (Mathf.Abs(moveDirection.x) < 0.01f)
            faceX = moveDirection.y >= 0f ? 1f : -1f; // mostly vertical: use up/down for facing
        transform.localScale = new Vector3(faceX * Mathf.Abs(s.x), s.y, s.z);
    }

    /// <summary>Optional: initialize direction and optionally damage/speed for one call from spawner.</summary>
    public void Initialize(Vector2 direction, int? damageOverride = null, float? speedOverride = null)
    {
        SetDirection(direction);
        if (damageOverride.HasValue) damage = damageOverride.Value;
        if (speedOverride.HasValue) speed = speedOverride.Value;
    }

    private void FixedUpdate()
    {
        if (_impacted) return;

        float step = speed * Time.fixedDeltaTime;
        _rb.MovePosition(_rb.position + moveDirection * step);
        _distanceTraveled += step;

        bool timeout = maxLifetime > 0f && (Time.time - _spawnTime) >= maxLifetime;
        bool overDistance = maxDistance > 0f && _distanceTraveled >= maxDistance;
        if (timeout || overDistance)
            Dissipate();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other, "OnTriggerEnter2D");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleHit(collision.collider, "OnCollisionEnter2D");
    }

    private void HandleHit(Collider2D other, string source)
    {
        if (_impacted) return;

        // Ignore the owner (e.g. boss projectile must not hit the boss when it spawns inside their collider)
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
        {
            Debug.Log($"Projectile ignored hit on owner '{other.name}'");
            return;
        }

        Debug.Log($"Projectile hit via {source} on '{other.name}' (isPlayerProjectile={isPlayerProjectile})");

        if (isPlayerProjectile)
        {
            var boss = other.GetComponentInParent<BubbleBlumBoss>();
            if (boss != null)
            {
                // If Bubble Blum's bubble shield is active (slam pattern), projectiles hit the shield:
                // - They bounce off
                // - They don't damage the boss
                // - They count toward breaking the shield early
                if (boss.isBubbleShieldActive)
                {
                    boss.OnShieldHitByProjectile();
                    // Reflect horizontally away from the boss for a simple bounce effect
                    moveDirection = new Vector2(-moveDirection.x, moveDirection.y).normalized;
                    Debug.Log($"Projectile bounced off shielded Boss '{boss.name}'");
                    return;
                }

                Debug.Log($"Projectile dealing {damage} damage to Boss '{boss.name}'");
                boss.TakeDamage(damage, transform.position, knockbackForceOnEnemy);
                Impact();
                return;
            }
        }
        else
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                Debug.Log($"Projectile dealing {damage} damage to Player");
                playerHealth.TakeDamage(damage, transform.position, knockbackForceOnPlayer);
                Impact();
                return;
            }
        }
    }

    private void Impact()
    {
        if (_impacted) return;
        _impacted = true;
        SetImpactTriggerSafe();
        Destroy(gameObject, impactDestroyDelay);
    }

    private void Dissipate()
    {
        if (_impacted) return;
        _impacted = true;
        SetImpactTriggerSafe();
        Destroy(gameObject, impactDestroyDelay);
    }

    void SetImpactTriggerSafe()
    {
        SetAnimatorTriggerSafe(triggerImpactedProjectile);
    }

    // --- Static limit per type ---
    private static bool TryRegister(string typeId, int maxOnScreen)
    {
        if (string.IsNullOrEmpty(typeId)) return true;
        if (!s_activeCountByType.ContainsKey(typeId))
            s_activeCountByType[typeId] = 0;
        if (s_activeCountByType[typeId] >= maxOnScreen)
            return false;
        s_activeCountByType[typeId]++;
        return true;
    }

    private static void Unregister(string typeId)
    {
        if (string.IsNullOrEmpty(typeId)) return;
        if (s_activeCountByType.ContainsKey(typeId) && s_activeCountByType[typeId] > 0)
            s_activeCountByType[typeId]--;
    }

    /// <summary>Current number of active projectiles of this type (for UI or upgrades).</summary>
    public static int GetActiveCount(string typeId)
    {
        return s_activeCountByType.TryGetValue(typeId, out int c) ? c : 0;
    }
}
