using System.Collections;
using UnityEngine;

/// <summary>
/// Bubble Blum Boss - AI controller for the Bubble Blum boss fight.
/// Updated to use the new CharacterAnimator system with Light/Heavy attacks.
/// </summary>
public class BubbleBlumBoss : MonoBehaviour
{
    [Header("References")]
    public Transform groundCheck;               // small transform at boss feet
    public LayerMask groundLayer;
    public Transform spawnPoint; 
    public CharacterAnimator characterAnimator; // Handles all animation triggers

    [Header("Tuning")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    public float jumpHorizontalSpeed = 4f;      // when jumping toward a target
    public float attackCooldown = 1.2f;
    public int maxHealth = 50;
    public int damage = 10;

    // Distances (tweak these)
    [Header("Attack Distances")]
    public float groundLightAttackDistance = 1.5f;  // Light attack: very close
    public float groundHeavyAttackDistance = 3.0f;  // Heavy attack: bit farther (calf kick)
    public float airLightAttackDistance = 2.0f;     // Air light: close in air (forward punch)
    public float airHeavyAttackDistance = 4.0f;    // Air heavy: dive range (aims down)

    [Header("Projectile Attack")]
    public Projectile bossProjectilePrefab;         // projectile the boss fires
    public Transform projectileFirePoint;           // where the projectile comes from
    public float projectileFireInterval = 5f;       // time between automatic shots

    [Header("Retreat Settings")]
    public float retreatDistance = 2f;              // when this close, boss jumps away instead of closing in
    public float retreatJumpHorizontalSpeed = 4f;   // horizontal speed when jumping away
    public float retreatJumpVerticalForce = 12f;    // vertical force when jumping away
    
    [Header("No-damage Jump (reposition)")]
    [Tooltip("Time in seconds since last hit before boss does a big jump away.")]
    public float noDamageJumpDelay = 6f;
    [Tooltip("Horizontal speed when performing the no-damage jump away from the player.")]
    public float noDamageJumpHorizontalSpeed = 6f;
    [Tooltip("Vertical force when performing the no-damage jump.")]
    public float noDamageJumpVerticalForce = 12f;

    [Header("Follow-jump (far)")]
    public float followJumpDelay = 2f;          // wait time when far before jumping to last known pos

    [Header("Demo: Jump-chase & patterns")]
    [Tooltip("Distance (units) left or right of player to land after jump-chase.")]
    public float jumpChaseLandOffset = 6f;
    [Tooltip("Height above player (Y offset) for each slam in the heavy pattern.")]
    public float slamHeightAbovePlayer = 7f;
    [Tooltip("Seconds between each slam in the heavy pattern.")]
    public float slamInterval = 1f;
    [Tooltip("Seconds to pause at the peak of the slam jump before falling (visual hang time).")]
    public float slamPauseAtPeak = 0.25f;
    [Tooltip("Height (units) she jumps for the ActivateBubbleShield intro before the first slam.")]
    public float shieldActivateJumpHeight = 2f;
    [Header("Pattern Cooldowns")]
    [Tooltip("Cooldown (seconds) before the big-bubble light attack can be used again.")]
    public float bigBubbleCooldown = 5f;
    [Tooltip("Cooldown (seconds) before the BubbleShield slam sequence can be used again after it finishes (including pop).")]
    public float slamCooldown = 5f;

    [Header("Pattern Choice")]
    [Tooltip("When both Big Bubble and Slam are off cooldown, chance (0-1) to choose the Slam pattern instead of Big Bubble.")]
    [Range(0f, 1f)] public float slamChanceWhenBothReady = 0.5f;

    [Header("Big Bubble (light pattern)")]
    [Tooltip("Minimum number of BubbleShot attacks the boss can fire in one Big Bubble pattern.")]
    public int minBubbleShotsPerPattern = 1;
    [Tooltip("Maximum number of BubbleShot attacks the boss can fire in one Big Bubble pattern.")]
    public int maxBubbleShotsPerPattern = 3;
    [Tooltip("Telegraph delay (seconds) for each of the two \"blow\" telegraphs before big bubble. Set this to roughly match the BubbleShotCharge animation length.")]
    public float bigBubbleTelegraphDelay = 0.7f;
    [Tooltip("Radius of shockwave on slam impact and shield pop.")]
    public float shockwaveRadius = 2f;
    [Tooltip("Damage dealt by slam/shield pop shockwave.")]
    public int shockwaveDamage = 8;
    [Tooltip("Knockback force used specifically for the final BubbleShieldPop shockwave.")]
    public float popShockwaveKnockbackForce = 12f;
    [Tooltip("How long (seconds) the BubbleShieldPop effect lasts before applying its shockwave damage.")]
    public float bubbleShieldPopDuration = 0.7f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.12f;

    [Header("Falling")]
    [Tooltip("Extra gravity when falling (1 = normal gravity).")]
    public float fallMultiplier = 2.5f;

    [Header("Damage Response (hit feedback)")]
    [Tooltip("How long the hit flicker lasts.")]
    public float flickerDuration = 0.12f;
    [Tooltip("Color to tint the sprite when hit. Red is visible when default tint is white.")]
    public Color flickerColor = Color.red;
    [Tooltip("Knockback force when hit by projectiles. Adjustable for tuning/upgrades.")]
    public float knockbackForce = 8f;
    [Header("Body contact (no damage)")]
    [Tooltip("When player and boss touch from movement (not an attack), both are pushed apart by this force. No damage.")]
    public float bodyBumpForce = 6f;
    [Tooltip("Vertical hop force when bumping into the player to avoid getting stuck together.")]
    public float bodyBumpHopForce = 4f;
    [Tooltip("Knockback force when the boss collides with the player mid-slam (before hitting the ground). Tune so it feels like ~4 units of pushback.")]
    public float slamDirectHitKnockbackForce = 10f;

    [Header("Bounce")]
    [Tooltip("Multiplier for the speed of all bounce arcs (bounce off player from slam, bounce up before pop). 1 = normal, >1 = faster, <1 = slower.")]
    public float bounceSpeedMultiplier = 1f;
    [Tooltip("Horizontal distance (units) to bounce away from the player when a slam directly hits them.")]
    public float slamBounceAwayDistance = 4f;
    [Tooltip("Vertical height (units) of the bounce arc when a slam hits the player.")]
    public float slamBounceHeight = 2f;
    [Tooltip("Vertical height (units) of the final bounce after the last slam, before BubbleShieldPop.")]
    public float finalSlamBounceHeight = 3f;

    [Header("Bubble Shield Break")]
    [Tooltip("Number of player projectiles that must hit the bubble shield to break it early.")]
    public int bubbleShieldHitsToBreak = 6;

    // internal
    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color _originalColor;
    private Coroutine _flickerCoroutine;

    private float attackTimer = 0f;
    private float projectileFireTimer = 0f;
    private float timeSinceLastDamage = 0f;
    private int currentHealth;
    private Vector2 playerLastKnownPos;

    // states
    private bool isGrounded = true;
    private bool isRecovering = false;
    private bool isHurt = false;
    private bool isAttacking = false;
    private bool isJumping = false;
    // private Coroutine followJumpCoroutine = null; // for future advanced boss: follow-jump when far
    private bool isPerformingPattern = false;
    private float bigBubbleCooldownTimer = 0f;
    private float slamCooldownTimer = 0f;
    // Shield / pattern state (used so projectiles can bounce off during slam pattern, shield break, and Big Bubble cancel)
    [HideInInspector] public bool isBubbleShieldActive = false;
    private bool isChargingBigBubble = false;
    private bool cancelBigBubbleToSlam = false;
    private int bubbleShieldHitCount = 0;
    private bool isBreakingBubbleShield = false;
    private bool lastSlamShockwaveHitPlayer = false;

    // player find retry
    private float findRetryInterval = 0.5f;
    private float findRetryTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) _originalColor = spriteRenderer.color;
        originalScale = transform.localScale;
        currentHealth = maxHealth;
    }

    void Start()
    {
        TryFindPlayer();
    }

    void Update()
    {
        // re-acquire player if missing
        if (player == null)
        {
            findRetryTimer -= Time.deltaTime;
            if (findRetryTimer <= 0f)
            {
                findRetryTimer = findRetryInterval;
                TryFindPlayer();
            }
            return;
        }

        // update basic timers and states
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (bigBubbleCooldownTimer > 0f) bigBubbleCooldownTimer -= Time.deltaTime;
        if (slamCooldownTimer > 0f) slamCooldownTimer -= Time.deltaTime;
        timeSinceLastDamage += Time.deltaTime;

        // automatic projectile fire on a timer (only when not in a pattern)
        if (bossProjectilePrefab != null && projectileFirePoint != null && !isPerformingPattern)
        {
            projectileFireTimer -= Time.deltaTime;
            if (projectileFireTimer <= 0f && !isRecovering && !isHurt && !isAttacking)
            {
                FireProjectileAtPlayer();
                projectileFireTimer = projectileFireInterval;
            }
        }

        // update grounded state
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            isGrounded = false; //Default to false if groundCheck not assigned
            Debug.LogWarning("BubbleBlumBoss: groundCheck is not assigned!");
        }
        
        // Update CharacterAnimator with grounded state (needed for auto ground/air attack selection)
        characterAnimator?.SetGrounded(isGrounded);

        // apply extra gravity when falling so boss doesn't float
        if (rb != null && rb.linearVelocity.y < 0f && fallMultiplier > 1f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        }

        // do nothing if recovering/hurt/attacking
        if (isRecovering || isHurt || isAttacking) return;

        // Demo: do nothing while a pattern is running (jump-chase + attack)
        if (isPerformingPattern) return;

        // record player's last known position for jump-based attacks
        playerLastKnownPos = player.position;

        // Always face the player (no matter what state she's in)
        FacePlayer();

        // No-damage jump: commented out for demo so it doesn't block jump-chase. Re-enable for advanced boss.
        // if (isGrounded && !isJumping && timeSinceLastDamage >= noDamageJumpDelay)
        // {
        //     StartCoroutine(NoDamageJumpAway());
        //     timeSinceLastDamage = 0f;
        //     return;
        // }

        // Retreat jump is not in the demo flow (removed from Update); RetreatJumpAndShoot coroutine still exists for later.

        // Demo: only movement is jump-chase. Requires isGrounded and !isJumping – if she still doesn't move, check groundCheck is assigned and groundLayer includes the floor.
        if (isGrounded && !isJumping)
        {
            StartCoroutine(JumpChaseThenAttack());
        }
    }

    /// <summary>Turn the boss to face the player (uses originalScale so flipping is consistent).</summary>
    void FacePlayer()
    {
        if (player == null) return;
        float dirXToPlayer = player.position.x - transform.position.x;
        if (Mathf.Abs(dirXToPlayer) > 0.01f)
        {
            float facingSign = Mathf.Sign(dirXToPlayer);
            transform.localScale = new Vector3(facingSign * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
    }

    // -----------------------------
    // FIND PLAYER
    // -----------------------------
    void TryFindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            Debug.Log("✅ BubbleBlumBoss: Player found.");
        }
    }

    // -----------------------------
    // CHASE (disabled for demo; movement is jump-chase only)
    // -----------------------------
    void Chase()
    {
        // Linear run chase disabled for demo.
        // if (player == null) return;
        // Vector2 direction = (player.position - transform.position).normalized;
        // characterAnimator?.SetMoving(true);
        // rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
        // if (Mathf.Abs(direction.x) > 0.01f) transform.localScale = new Vector3(Mathf.Sign(direction.x) * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
    }

    // -----------------------------
    // Demo: Jump-chase then choose one attack
    // -----------------------------
    IEnumerator JumpChaseThenAttack()
    {
        if (player == null) yield break;

        isPerformingPattern = true;

        // Jump toward a spot ~6 units left or right of player, land there
        yield return StartCoroutine(JumpChaseToPlayer());

        // Always face the player before choosing the next attack
        FacePlayer();

        // After landing, choose pattern based on cooldowns:
        // - Big bubble (light) uses bigBubbleCooldownTimer
        // - Slam sequence uses slamCooldownTimer (and BubbleShieldPop)
        bool canBigBubble = bigBubbleCooldownTimer <= 0f;
        bool canSlam = slamCooldownTimer <= 0f;

        if (canBigBubble && !canSlam)
        {
            // Only Big Bubble ready
            yield return StartCoroutine(BigBubbleAttack());
        }
        else if (!canBigBubble && canSlam)
        {
            // Only Slam ready
            yield return StartCoroutine(ShieldSlamPattern());
        }
        else if (canBigBubble && canSlam)
        {
            // Both ready: pick based on adjustable chance
            float roll = Random.value;
            bool chooseSlam = roll < slamChanceWhenBothReady;
            if (chooseSlam)
                yield return StartCoroutine(ShieldSlamPattern());
            else
                yield return StartCoroutine(BigBubbleAttack());
        }
        else
        {
            // Neither pattern ready: just end the pattern after the jump-chase reposition
            // (boss will start another jump-chase when cooldowns finish)
        }

        isPerformingPattern = false;
    }

    IEnumerator JumpChaseToPlayer()
    {
        if (player == null || rb == null) yield break;

        isJumping = true;
        attackTimer = attackCooldown;
        characterAnimator?.SetMoving(true);
        characterAnimator?.SetBubbleJumping(true);

        float side = Mathf.Sign(player.position.x - transform.position.x);
        if (Mathf.Abs(player.position.x - transform.position.x) < 0.5f) side = Random.value > 0.5f ? 1f : -1f;
        float targetX = player.position.x + side * jumpChaseLandOffset;
        float dx = targetX - transform.position.x;
        float hDir = Mathf.Sign(dx);

        rb.linearVelocity = new Vector2(hDir * jumpHorizontalSpeed, jumpForce);
        if (Mathf.Abs(hDir) > 0.01f)
            transform.localScale = new Vector3(hDir * Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);

        Debug.Log("[BubbleBlumBoss] JumpChase: triggering BubbleJump");
        characterAnimator?.TriggerByName("BubbleJump");
        yield return new WaitForSeconds(0.12f);
        characterAnimator?.SetGrounded(false);

        // Wait until we're grounded again (with timeout)
        float waitStart = Time.time;
        while (!isGrounded && (Time.time - waitStart) < 3f)
            yield return null;

        isJumping = false;
        characterAnimator?.SetBubbleJumping(false);
    }

    IEnumerator BigBubbleAttack()
    {
        if (player == null) yield break;

        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        int shots = Mathf.Clamp(Random.Range(minBubbleShotsPerPattern, maxBubbleShotsPerPattern + 1), 1, Mathf.Max(1, maxBubbleShotsPerPattern));
        cancelBigBubbleToSlam = false;
        isChargingBigBubble = true;
        characterAnimator?.SetBubbleShotCharging(true);

        for (int i = 0; i < shots; i++)
        {
            if (cancelBigBubbleToSlam) break;

            // First blow telegraph
            characterAnimator?.TriggerByName("BubbleShotCharge");
            float t = 0f;
            while (t < bigBubbleTelegraphDelay)
            {
                if (cancelBigBubbleToSlam) break;
                t += Time.deltaTime;
                yield return null;
            }
            if (cancelBigBubbleToSlam) break;

            // Second blow telegraph
            characterAnimator?.TriggerByName("BubbleShotCharge");
            t = 0f;
            while (t < bigBubbleTelegraphDelay)
            {
                if (cancelBigBubbleToSlam) break;
                t += Time.deltaTime;
                yield return null;
            }
            if (cancelBigBubbleToSlam) break;

            // Fire the bubble shot
            characterAnimator?.TriggerByName("BubbleShot");
            FireProjectileAtPlayer();

            // Tiny gap between shots (optional) – can be adjusted by changing this value
            if (i < shots - 1)
            {
                float gap = 0.1f;
                t = 0f;
                while (t < gap)
                {
                    if (cancelBigBubbleToSlam) break;
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        bigBubbleCooldownTimer = bigBubbleCooldown;

        // If we were interrupted mid-charge, immediately roll into Slam instead of finishing Big Bubble
        if (cancelBigBubbleToSlam)
        {
            cancelBigBubbleToSlam = false;
            isChargingBigBubble = false;
            characterAnimator?.SetBubbleShotCharging(false);
            isAttacking = false;
            yield return StartCoroutine(ShieldSlamPattern());
            yield break;
        }

        // Normal end of Big Bubble pattern
        yield return new WaitForSeconds(0.5f);
        isChargingBigBubble = false;
        characterAnimator?.SetBubbleShotCharging(false);
        isAttacking = false;
    }

    IEnumerator ShieldSlamPattern()
    {
        if (player == null || rb == null) yield break;

        isAttacking = true;
        isJumping = true;
        isBubbleShieldActive = true;
        characterAnimator?.SetBubbleShieldActive(true);
        characterAnimator?.SetGrounded(false);
        bubbleShieldHitCount = 0;
        isBreakingBubbleShield = false;

        float g = Mathf.Abs(Physics2D.gravity.y);
        if (g < 0.1f) g = 20f;

        // Intro: jump 2 units in the air, play ActivateBubbleShield, fall back down once, then start chained slams
        float activateVy = Mathf.Sqrt(2f * g * shieldActivateJumpHeight);
        rb.linearVelocity = new Vector2(0f, activateVy);
        Debug.Log("[BubbleBlumBoss] ShieldSlam: ActivateBubbleShield (intro jump)");
        characterAnimator?.TriggerByName("ActivateBubbleShield");

        float tLand = Time.time;
        while (!isGrounded && (Time.time - tLand) < 4f) yield return null;

        isJumping = false;
        characterAnimator?.SetGrounded(true);
        yield return new WaitForSeconds(0.15f);

        // Start first upward arc toward the player
        isJumping = true;
        characterAnimator?.SetGrounded(false);
        characterAnimator?.SetBubbleJumping(true);
        characterAnimator?.TriggerByName("BubbleShieldJump");

        for (int i = 0; i < 3; i++)
        {
            // Compute arc from current position up to a point above the player
            float targetX = player.position.x;
            float targetY = player.position.y + slamHeightAbovePlayer;
            float heightGain = targetY - transform.position.y;
            if (heightGain < 0.5f) heightGain = 0.5f;

            float vy = Mathf.Sqrt(2f * g * heightGain);
            float tApex = vy / g;
            float dx = targetX - transform.position.x;
            float vx = (tApex > 0.001f) ? (dx / tApex) : 0f;

            rb.linearVelocity = new Vector2(vx, vy);

            // Wait until we start coming down
            while (rb.linearVelocity.y > 0.1f) yield return null;

            Debug.Log($"[BubbleBlumBoss] ShieldSlam: slam {i + 1}/3 BubbleShieldSlam (falling)");
            characterAnimator?.TriggerByName("BubbleShieldSlam");

            // Small hang at peak of the slam motion
            rb.linearVelocity = Vector2.zero;
            float savedGravity = rb.gravityScale;
            rb.gravityScale = 0f;
            yield return new WaitForSeconds(slamPauseAtPeak);
            rb.gravityScale = savedGravity;

            // Fall to ground
            float t0 = Time.time;
            while (!isGrounded && (Time.time - t0) < 4f) yield return null;

            isJumping = false;
            characterAnimator?.SetGrounded(true);
            DealSlamShockwaveDamage();

            // If the shield is being broken early, exit the pattern now (BreakBubbleShieldEarly handles the pop)
            if (isBreakingBubbleShield)
                yield break;

            // If this slam's shockwave actually hit the player, bounce off and land slamBounceAwayDistance units away
            if (lastSlamShockwaveHitPlayer)
            {
                // Bounce arc handles its own jump/land; after that we'll start the next jump from the bounce landing.
                yield return StartCoroutine(BounceOffPlayerFromSlam());
                lastSlamShockwaveHitPlayer = false;
            }

            // Chain directly into the next upward jump (no grounded pause) except after the last slam
            if (i < 2)
            {
                isJumping = true;
                characterAnimator?.SetGrounded(false);
                characterAnimator?.SetBubbleJumping(true);
                characterAnimator?.TriggerByName("BubbleShieldJump");
                // optional tiny delay so the animator can register the new jump state
                if (slamInterval > 0f)
                    yield return new WaitForSeconds(slamInterval);
            }
        }

        // Final bounce off the ground before the pop
        yield return StartCoroutine(BounceUpBeforePop());

        // If the shield was already broken early, ShieldSlamPattern exits without playing pop again.
        if (!isBreakingBubbleShield)
        {
            Debug.Log("[BubbleBlumBoss] ShieldSlam: BubbleShieldPop (final shockwave)");
            characterAnimator?.TriggerByName("BubbleShieldPop");
            // Let the pop animation/effect play for bubbleShieldPopDuration seconds, then apply the shockwave
            yield return new WaitForSeconds(bubbleShieldPopDuration);
            DealPopShockwaveDamage();
            slamCooldownTimer = slamCooldown;
            isBubbleShieldActive = false;
            characterAnimator?.SetBubbleShieldActive(false);
            characterAnimator?.SetBubbleJumping(false);
            isAttacking = false;
        }
    }

    void DealSlamShockwaveDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        bool hitPlayer = false;
        foreach (var c in hits)
        {
            var health = c.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(shockwaveDamage, transform.position, knockbackForce);
                hitPlayer = true;
                break;
            }
        }
        lastSlamShockwaveHitPlayer = hitPlayer;
    }

    void DealPopShockwaveDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        foreach (var c in hits)
        {
            var health = c.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(shockwaveDamage, transform.position, popShockwaveKnockbackForce);
                break;
            }
        }
    }

    public void OnShieldHitByProjectile()
    {
        if (!isBubbleShieldActive || bubbleShieldHitsToBreak <= 0) return;
        bubbleShieldHitCount++;
        if (bubbleShieldHitCount >= bubbleShieldHitsToBreak)
        {
            // Trigger forced shield break if not already breaking
            if (!isBreakingBubbleShield)
            {
                StartCoroutine(BreakBubbleShieldEarly());
            }
        }
    }

    IEnumerator BreakBubbleShieldEarly()
    {
        if (!isBubbleShieldActive || isBreakingBubbleShield) yield break;
        isBreakingBubbleShield = true;

        Debug.Log("[BubbleBlumBoss] Bubble shield broken early by player projectiles. Forcing BubbleShieldPop.");

        // Immediately play BubbleShieldPop from current position
        characterAnimator?.TriggerByName("BubbleShieldPop");

        yield return new WaitForSeconds(bubbleShieldPopDuration);
        DealPopShockwaveDamage();
        slamCooldownTimer = slamCooldown;
        isBubbleShieldActive = false;
        characterAnimator?.SetBubbleShieldActive(false);
        characterAnimator?.SetBubbleJumping(false);
        isAttacking = false;
        isBreakingBubbleShield = false;
    }

    IEnumerator BounceOffPlayerFromSlam()
    {
        if (player == null || rb == null) yield break;

        float g = Mathf.Abs(Physics2D.gravity.y);
        if (g < 0.1f) g = 20f;

        float awaySign = Mathf.Sign(transform.position.x - player.position.x);
        if (Mathf.Abs(awaySign) < 0.01f) awaySign = 1f;
        float targetX = player.position.x + awaySign * slamBounceAwayDistance;

        float heightGain = slamBounceHeight;
        if (heightGain < 0.5f) heightGain = 0.5f;

        float vy = Mathf.Sqrt(2f * g * heightGain);
        float tApex = vy / g;
        float dx = targetX - transform.position.x;
        float totalTime = 2f * tApex;
        float vx = (totalTime > 0.001f) ? (dx / totalTime) : 0f;

        isJumping = true;
        characterAnimator?.SetGrounded(false);
        // Use the same animation as the slam for the bounce-off arc
        characterAnimator?.TriggerByName("BubbleShieldSlam");

        rb.linearVelocity = new Vector2(vx, vy);

        float t0 = Time.time;
        while (!isGrounded && (Time.time - t0) < 4f)
            yield return null;

        isJumping = false;
        characterAnimator?.SetGrounded(true);
    }

    IEnumerator BounceUpBeforePop()
    {
        if (rb == null) yield break;

        float g = Mathf.Abs(Physics2D.gravity.y);
        if (g < 0.1f) g = 20f;

        float heightGain = finalSlamBounceHeight;
        if (heightGain < 0.5f) heightGain = 0.5f;

        float vy = Mathf.Sqrt(2f * g * heightGain);

        isJumping = true;
        characterAnimator?.SetGrounded(false);
        characterAnimator?.SetBubbleJumping(true);
        // Use the same animation as the slam for the bounce off the ground
        characterAnimator?.TriggerByName("BubbleShieldSlam");

        float mult = Mathf.Max(0.01f, bounceSpeedMultiplier);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, vy * mult);

        float t0 = Time.time;
        while (!isGrounded && (Time.time - t0) < 4f)
            yield return null;

        isJumping = false;
        characterAnimator?.SetGrounded(true);
        characterAnimator?.SetBubbleJumping(false);
    }

    // Body contact: when player and boss touch from movement (not from an attack), knockback both, no damage.
    void OnCollisionEnter2D(Collision2D collision)
    {
        var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        Vector2 awayFromBoss = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
        if (awayFromBoss.sqrMagnitude < 0.01f) awayFromBoss = Vector2.right;

        // If we're in the middle of a downward slam with the bubble shield active, this counts as a direct hit:
        // push the player back hard and deal damage.
        if (isBubbleShieldActive && isJumping && rb != null && rb.linearVelocity.y < 0f)
        {
            float hitKnock = slamDirectHitKnockbackForce > 0f ? slamDirectHitKnockbackForce : knockbackForce;
            playerHealth.TakeDamage(shockwaveDamage, transform.position, hitKnock);

            // Optional: give the boss a small upward nudge so they don't stick to the player
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, bodyBumpHopForce));
            return;
        }

        // Otherwise, normal body bump: both pushed apart, no damage.
        if (rb != null)
            rb.linearVelocity += new Vector2(-awayFromBoss.x * bodyBumpForce, bodyBumpHopForce);
        var playerRb = collision.rigidbody ?? collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.linearVelocity += new Vector2(awayFromBoss.x * bodyBumpForce, bodyBumpHopForce * 0.5f);
    }

    // -----------------------------
    // GROUND ATTACKS (legacy; demo uses BigBubbleAttack / ShieldSlamPattern from jump-chase)
    // -----------------------------
    // -----------------------------
    void GroundLightAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        
        // CharacterAnimator automatically chooses ground animation since isGrounded is true
        characterAnimator?.TriggerLightAttack();

        Debug.Log("Ground Light Attack (very close) triggered.");
        // Attack hitbox should be spawned by an AnimationEvent inside the clip
        StartCoroutine(EndAttackAfter(0.6f));
    }

    void GroundHeavyAttack()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        
        // CharacterAnimator automatically chooses ground animation since isGrounded is true
        characterAnimator?.TriggerHeavyAttack();

        Debug.Log("Ground Heavy Attack (calf kick) triggered.");
        StartCoroutine(EndAttackAfter(0.8f));
    }

    // -----------------------------
    // AIR ATTACKS helper coroutines
    // -----------------------------
    IEnumerator JumpThenPerformAirLight()
    {
        // Prepare to jump
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.SetMoving(true); // transition to jump

        // compute horizontal direction toward player
        float dx = playerLastKnownPos.x - transform.position.x;
        float hdir = Mathf.Sign(dx);
        rb.linearVelocity = new Vector2(hdir * jumpHorizontalSpeed, jumpForce);

        // small delay to become airborne
        yield return new WaitForSeconds(0.12f);

        // Update grounded state (now in air)
        characterAnimator?.SetGrounded(false);
        
        // now perform Air Light - CharacterAnimator will auto-detect air since isGrounded is false
        characterAnimator?.TriggerLightAttack();
        Debug.Log("Air Light Attack triggered (forward air punch).");

        // let the animation/hitbox play out
        yield return new WaitForSeconds(0.6f);

        // recover on landing - let physics handle landing detection elsewhere
        isJumping = false;
        isAttacking = false;
    }

    IEnumerator RetreatJumpAndShoot()
    {
        if (player == null) yield break;

        // set state
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.SetMoving(true);

        // figure out which way to jump (away from player)
        float dx = player.position.x - transform.position.x;
        float awayDir = -Mathf.Sign(dx == 0 ? 1f : dx);

        // apply jump velocity away from player
        rb.linearVelocity = new Vector2(awayDir * retreatJumpHorizontalSpeed, retreatJumpVerticalForce);
        Debug.Log($"[BubbleBlumBoss] RetreatJumpAndShoot started. awayDir={awayDir}, vel={rb.linearVelocity}");

        // short delay so we're clearly in the air before shooting
        yield return new WaitForSeconds(0.12f);

        characterAnimator?.SetGrounded(false);

        // fire a projectile while jumping away
        FireProjectileAtPlayer();

        // let the jump arc play out a bit
        yield return new WaitForSeconds(0.6f);

        isJumping = false;
        isAttacking = false;
        Debug.Log("[BubbleBlumBoss] RetreatJumpAndShoot finished.");
    }

    IEnumerator NoDamageJumpAway()
    {
        if (player == null) yield break;

        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.SetMoving(true);

        // jump away from player horizontally
        float dx = player.position.x - transform.position.x;
        float awayDir = -Mathf.Sign(dx == 0 ? 1f : dx);

        rb.linearVelocity = new Vector2(awayDir * noDamageJumpHorizontalSpeed, noDamageJumpVerticalForce);

        // small delay to let us get airborne
        yield return new WaitForSeconds(0.12f);

        characterAnimator?.SetGrounded(false);

        // let the jump arc play out a bit
        yield return new WaitForSeconds(0.6f);

        isJumping = false;
        isAttacking = false;
    }

    // delayed follow-jump (far) - record last known pos at delay start
    IEnumerator DelayedFollowJump(Vector2 startingPlayerPos, float delay)
    {
        // ensure only one follow-jump coroutine runs
        Vector2 targetPos = startingPlayerPos;
        float timer = 0f;
        while (timer < delay)
        {
            // keep updating targetPos to player's latest position while waiting
            if (player != null) targetPos = player.position;
            timer += Time.deltaTime;
            yield return null;
        }

        // followJumpCoroutine = null; // clear reference (when re-enabling followJumpCoroutine)
        // After delay, perform a jump toward targetPos
        StartCoroutine(JumpThenPerformAirLightOrHeavyDependingOnPosition(targetPos));
        yield break;
    }

    IEnumerator JumpThenPerformAirLightOrHeavyDependingOnPosition(Vector2 targetPos)
    {
        // similar logic as JumpThenPerformAirLight: choose Light based on airborne/grounded position
        isJumping = true;
        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.SetMoving(true);

        float dx = targetPos.x - transform.position.x;
        float hSign = Mathf.Sign(dx);
        rb.linearVelocity = new Vector2(hSign * jumpHorizontalSpeed, jumpForce);

        yield return new WaitForSeconds(0.12f);

        // Update grounded state (now in air)
        characterAnimator?.SetGrounded(false);

        // choose based on whether target is airborne near us or on ground near us
        float horDist = Mathf.Abs(targetPos.x - transform.position.x);
        float vertDiff = targetPos.y - transform.position.y;

        if (vertDiff > 0.5f && horDist <= airLightAttackDistance)
        {
            // CharacterAnimator will auto-detect air since isGrounded is false
            characterAnimator?.TriggerLightAttack();
            Debug.Log("Follow-jump: performed Air Light on target.");
        }
        // otherwise default to Air Light
        else
        {
            characterAnimator?.TriggerLightAttack();
            Debug.Log("Follow-jump defaulted to Air Light.");
        }

        yield return new WaitForSeconds(0.6f);

        isJumping = false;
        isAttacking = false;
        yield break;
    }

    // -----------------------------
    // RECOVER
    // -----------------------------
    public void Recover()
    {
        if (isRecovering) return;
        isRecovering = true;
        rb.linearVelocity = Vector2.zero;
        
        // Recover trigger is commented out in CharacterAnimator for snappy feel
        // Uncomment if you want to add recovery animations later
        // characterAnimator?.TriggerRecover();
        
        Debug.Log("Recover triggered.");
        Invoke(nameof(EndRecover), 0.5f);
    }

    void EndRecover()
    {
        isRecovering = false;
    }

    // -----------------------------
    // HURT & DIE (sprite flicker + adjustable knockback)
    // -----------------------------
    public void TakeDamage(int amount)
    {
        TakeDamage(amount, default, 0f);
    }

    public void TakeDamage(int amount, Vector2 hitFromPosition, float knockbackForceOverride)
    {
        Debug.Log($"[BubbleBlumBoss] TakeDamage called. amount={amount}, hitFrom={hitFromPosition}, knockbackOverride={knockbackForceOverride}");
        if (currentHealth <= 0) return;
        // If we are currently charging Big Bubble, getting hit will cancel it and immediately force a slam sequence.
        if (isChargingBigBubble)
        {
            cancelBigBubbleToSlam = true;
        }
        if (CoreManager.Instance != null)
            CoreManager.Instance.AddCorePoint();
        currentHealth -= amount;
        Debug.Log("Boss takes " + amount + " damage. HP left: " + currentHealth);

        // reset no-damage timer
        timeSinceLastDamage = 0f;

        if (spriteRenderer != null)
        {
            if (_flickerCoroutine != null) StopCoroutine(_flickerCoroutine);
            _flickerCoroutine = StartCoroutine(FlickerTint());
        }

        isHurt = true;
        float force = knockbackForceOverride > 0f ? knockbackForceOverride : knockbackForce;
        if (force > 0f && rb != null && hitFromPosition != default)
        {
            Vector2 dir = ((Vector2)transform.position - hitFromPosition).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.left;
            rb.linearVelocity = dir * force;
        }
        else if (rb != null)
            rb.linearVelocity = Vector2.zero;
        Invoke(nameof(EndHurt), 0.25f);

        if (currentHealth <= 0)
            Die();
    }

    IEnumerator FlickerTint()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = flickerColor;
        yield return new WaitForSeconds(flickerDuration);
        spriteRenderer.color = _originalColor;
        _flickerCoroutine = null;
    }

    void EndHurt() { isHurt = false; }

    void Die()
    {
        // Record that this boss was beaten so the save slot can show their profile as beaten (darker)
        if (GameManager.Instance != null)
        {
            string bossName = !string.IsNullOrEmpty(GameManager.Instance.selectedBoss)
                ? GameManager.Instance.selectedBoss
                : "BubbleBlum";
            GameManager.Instance.RecordBossDefeated(bossName);
        }
        rb.linearVelocity = Vector2.zero;
        Debug.Log("Boss died!");
        Destroy(gameObject, 1f);
    }

    // -----------------------------
    // Small helpers
    // -----------------------------
    void FireProjectileAtPlayer()
    {
        if (bossProjectilePrefab == null || projectileFirePoint == null || player == null) return;

        Projectile proj = Instantiate(bossProjectilePrefab, projectileFirePoint.position, Quaternion.identity);

        Vector2 dir = (player.position - projectileFirePoint.position).normalized;
        proj.isPlayerProjectile = false;
        proj.owner = transform; // so the projectile does not hit the boss when it spawns inside their collider
        proj.SetDirection(dir);
        Debug.Log($"[BubbleBlumBoss] Fired projectile toward player. dir={dir}, prefab={bossProjectilePrefab.name}");
    }

    IEnumerator EndAttackAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

       // Optional: show attack ranges as debug spheres
       Gizmos.color = Color.red;
       Gizmos.DrawWireSphere(transform.position, groundLightAttackDistance);
       Gizmos.color = Color.magenta;
       Gizmos.DrawWireSphere(transform.position, groundHeavyAttackDistance);
       Gizmos.color = Color.cyan;
       Gizmos.DrawWireSphere(transform.position, airHeavyAttackDistance);
       Gizmos.color = Color.green;
       Gizmos.DrawWireSphere(transform.position, airLightAttackDistance);
    }
}
