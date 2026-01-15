using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player controller for Mega Man/Gunvolt-style action platformer.
/// Handles movement, jumping, and combat input. Simplified from fighting game version.
/// </summary>
public class PlayerController : MonoBehaviour
{
    // ============================================
    // PUBLIC VARIABLES (Set in Inspector)
    // ============================================
    
    [Header("References")]
    public CharacterAnimator characterAnimator;  // Handles animation triggers
    public Transform groundCheck;                 // Transform used to check if grounded
    public LayerMask groundLayer;                  // Which layers count as "ground"
    
    [Header("Movement")]
    public float moveSpeed = 5f;                  // Horizontal movement speed
    public float jumpForce = 10f;                  // Vertical force applied when jumping
    public float fallMultiplier = 2.5f;           // Makes falling faster for snappier feel
    public float groundCheckRadius = 0.12f;       // Size of circle used to detect ground
    
    [Header("Combat")]
    public float attackCooldown = 1.2f;           // Time between attacks (prevents spam)

    // ============================================
    // PRIVATE VARIABLES (Internal state)
    // ============================================
    
    private Rigidbody2D rb;                       // Physics body component
    private SpriteRenderer sr;                    // Used to flip sprite left/right
    private Vector2 moveInput;                    // Current input direction (-1 to 1)
    private bool isGrounded;                      // Whether player is touching ground
    private bool wasMoving = false;               // Previous frame's movement state (for animation optimization)
    private float attackTimer = 0f;               // Countdown timer for attack cooldown
    private bool isAttacking = false;             // Whether attack animation is playing

    private PlayerControls controls;              // Input system controls

    // ============================================
    // INITIALIZATION
    // ============================================
    
    /// <summary>
    /// Sets up input controls. Called before Start().
    /// </summary>
    void Awake()
    {
        controls = new PlayerControls();
        
        // Subscribe to input events once (prevents duplicate subscriptions)
        // These callbacks will fire whenever the input is pressed
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        controls.Player.Jump.performed += ctx => TryJump();

        // AttackA/AttackB input actions are treated as Light/Heavy attack buttons
        controls.Player.LightAttack.performed += ctx => TryLightAttack();
        controls.Player.HeavyAttack.performed += ctx => TryHeavyAttack();
        controls.Player.Heal.performed += ctx => TryHeal();
        controls.Player.Buff.performed += ctx => TryBuff();
        controls.Player.CoreBurst.performed += ctx => TryCoreBurst();

        // NOTE: Heal/Buff/CoreBurst don't have input actions yet.
        // After you add them to PlayerControls.inputactions, you can hook them up like:
        // controls.Player.Heal.performed      += ctx => TryHeal();
        // controls.Player.Buff.performed      += ctx => TryBuff();
        // controls.Player.CoreBurst.performed += ctx => TryCoreBurst();
    }

    /// <summary>
    /// Enables input when object becomes active. Only enables if game isn't paused.
    /// </summary>
    void OnEnable()
    {
        // Only enable controls if game isn't paused
        if (!PauseMenu.isPaused)
        {
            controls.Player.Enable();
        }
    }

    /// <summary>
    /// Disables input when object becomes inactive. Prevents input when disabled.
    /// </summary>
    void OnDisable()
    {
        if (controls != null)
            controls.Player.Disable();
    }

    /// <summary>
    /// Gets component references. Called after Awake().
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    // ============================================
    // UPDATE LOOP (Runs every frame)
    // ============================================
    
    /// <summary>
    /// Handles pause menu input blocking. Runs after Update().
    /// This ensures pause state is checked after all other updates.
    /// </summary>
    void LateUpdate()
    {
        // Disable player input when paused
        if (PauseMenu.isPaused && controls.Player.enabled)
        {
            controls.Player.Disable();
        }
        // Re-enable player input when unpaused
        else if (!PauseMenu.isPaused && !controls.Player.enabled)
        {
            controls.Player.Enable();
        }
    }

    /// <summary>
    /// Main update loop. Handles attack cooldown, ground detection, movement, and physics.
    /// </summary>
    void Update()
    {
        // Count down attack cooldown timer
        if (attackTimer > 0f) 
            attackTimer -= Time.deltaTime;
        
        // Check if player is touching the ground using a circle overlap
        // This is more reliable than collision detection alone
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        // Fallback: if groundCheck isn't assigned, use collision-based detection
        // (OnCollisionEnter/Exit methods below handle this)
        
        // Update animator with grounded state (affects jump/fall animations)
        characterAnimator?.SetGrounded(isGrounded);
        
        // Move player horizontally (always allow movement, even while attacking)
        // This gives Mega Man/Gunvolt feel where you can shoot while moving
        MovePlayer();
        
        // Apply enhanced falling physics for snappier jump feel
        ApplyBetterJump();
    }

    // ============================================
    // MOVEMENT
    // ============================================
    
    /// <summary>
    /// Handles horizontal movement and sprite flipping.
    /// In Mega Man style, player can move while attacking (unlike fighting games).
    /// </summary>
    void MovePlayer()
    {
        // Set horizontal velocity based on input, preserve vertical velocity
        // NOTE: Using rb.linearVelocity (see explanation at bottom of file)
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        
        // Flip sprite to face movement direction
        if (moveInput.x != 0)
            sr.flipX = moveInput.x < 0;  // Flip left if moving left
        
        // Update animation based on movement
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;  // Small threshold to avoid jitter
        
        // Only trigger animation state change when movement state actually changes
        // This prevents spamming animation triggers every frame
        if (isMoving != wasMoving)
        {
            characterAnimator?.SetMoving(isMoving);
            wasMoving = isMoving;
        }
        // Keep animator bool updated even if state didn't change
        // (some animators need the bool value, not just the trigger)
        else if (characterAnimator != null && characterAnimator.anim != null)
        {
            characterAnimator.anim.SetBool("IsMoving", isMoving);
        }
    }

    /// <summary>
    /// Makes falling faster for snappier, more responsive jump feel.
    /// Only applies extra gravity when falling (not when rising).
    /// </summary>
    void ApplyBetterJump()
    {
        if (rb == null) return;

        // Only apply extra gravity when falling (y velocity is negative)
        if (rb.linearVelocity.y < 0)
        {
            // Multiply gravity by fallMultiplier to make falling faster
            // Subtracting 1 because we're adding to existing gravity
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
    }

    /// <summary>
    /// Makes player jump if grounded. Called when jump button is pressed.
    /// </summary>
    void TryJump()
    {
        if (isGrounded)
        {
            // Set vertical velocity to jump force, preserve horizontal velocity
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            
            // Trigger jump animation
            characterAnimator?.SetMoving(true);
        }
    }

    // ============================================
    // COMBAT & ABILITIES (Light/Heavy/Heal/Buff/CoreBurst)
    // ============================================
    
    /// <summary>
    /// Light attack (was Attack A).
    /// - Can be used while moving or standing still.
    /// - Uses a shared cooldown so you can't spam it endlessly.
    /// </summary>
    void TryLightAttack()
    {
        // Prevent attack if still on cooldown or an ability is already playing
        if (attackTimer > 0f || isAttacking) 
            return;
        
        // Start ability
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // Do NOT touch velocity here – movement continues as normal
        
        // Trigger appropriate animation based on whether grounded or in air
        if (isGrounded)
        {
            characterAnimator?.TriggerGroundLightAttack();
        }
        else
        {
            characterAnimator?.TriggerAirLightAttack();
        }
        
        // End attack state after animation duration
        Invoke(nameof(EndAttack), 0.6f);
    }
    
    /// <summary>
    /// Heavy attack (was Attack B).
    /// Also does not stop movement, works both grounded and in air.
    /// </summary>
    void TryHeavyAttack()
    {
        // Prevent attack if still on cooldown or an ability is already playing
        if (attackTimer > 0f || isAttacking) 
            return;
        
        // Start ability
        isAttacking = true;
        attackTimer = attackCooldown;
        
        // Movement is NOT stopped – still free to run while attacking
        
        // Trigger appropriate animation
        if (isGrounded)
        {
            characterAnimator?.TriggerGroundHeavyAttack();
        }
        else
        {
            characterAnimator?.TriggerAirHeavyAttack();
        }
        
        // End attack state after animation duration (slightly longer than light)
        Invoke(nameof(EndAttack), 0.8f);
    }

    /// <summary>
    /// Heal ability.
    /// Currently only plays an animation hook – you can add HP logic later.
    /// </summary>
    void TryHeal()
    {
        if (attackTimer > 0f || isAttacking)
            return;

        isAttacking = true;
        attackTimer = attackCooldown;

        // No movement changes – can heal while moving or idle
        characterAnimator?.TriggerHeal();

        // Duration can be tweaked based on your animation length
        Invoke(nameof(EndAttack), 0.7f);
    }

    /// <summary>
    /// Buff ability (e.g., temporary attack/defense boost).
    /// Right now it's just an animation hook.
    /// </summary>
    void TryBuff()
    {
        if (attackTimer > 0f || isAttacking)
            return;

        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.TriggerBuff();

        Invoke(nameof(EndAttack), 0.7f);
    }

    /// <summary>
    /// Core Burst ability (your \"super\" move).
    /// Again, animation only for now – damage/effects can be added later.
    /// </summary>
    void TryCoreBurst()
    {
        if (attackTimer > 0f || isAttacking)
            return;

        isAttacking = true;
        attackTimer = attackCooldown;

        characterAnimator?.TriggerCoreBurst();

        // Usually supers take a bit longer
        Invoke(nameof(EndAttack), 1.0f);
    }
    
    /// <summary>
    /// Called when any attack/ability animation finishes. Allows new actions.
    /// </summary>
    void EndAttack()
    {
        isAttacking = false;
    }

    // ============================================
    // COLLISION DETECTION (Backup ground detection)
    // ============================================
    
    /// <summary>
    /// Backup ground detection using collision. Used if groundCheck isn't assigned.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    /// <summary>
    /// Backup ground detection - marks as not grounded when leaving ground collision.
    /// </summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}

/* ============================================
 * VELOCITY EXPLANATION (rb.linearVelocity vs rb.velocity)
 * ============================================
 * 
 * WHAT'S THE DIFFERENCE?
 * - rb.velocity: Standard Unity 2D property (Vector2) - works in all Unity versions
 * - rb.linearVelocity: Newer property from Unity's updated physics system
 * 
 * WHY DOES THIS CODE USE linearVelocity?
 * - Your boss script also uses linearVelocity, so your Unity version supports it
 * - linearVelocity separates linear (straight) movement from angular (rotation) movement
 * - This is useful for 2D games where you typically only care about linear movement
 * 
 * SHOULD YOU CHANGE IT?
 * - If your game compiles and runs fine: NO, keep it as is
 * - If you get errors about "linearVelocity not found": Change all instances to "velocity"
 * - If you want maximum compatibility: Change to "velocity" (works everywhere)
 * 
 * TO CHANGE: Replace all instances of:
 *   rb.linearVelocity
 * with:
 *   rb.velocity
 * 
 * Both do the same thing for 2D movement - linearVelocity is just more explicit.
 */
