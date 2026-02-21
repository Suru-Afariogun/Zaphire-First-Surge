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
    
    [Header("Core Abilities (Hold to Charge)")]
    public float healingCoreHoldDuration = 8f;    // Time in seconds to hold HealingCore button
    public float powerCoreHoldDuration = 8f;      // Time in seconds to hold PowerCore button

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

    // Core ability charging (hold-to-charge mechanic)
    private float healingCoreHoldTimer = 0f;      // Current hold time for HealingCore
    private float powerCoreHoldTimer = 0f;        // Current hold time for PowerCore
    private bool isHoldingHealingCore = false;    // Whether HealingCore button is currently held
    private bool isHoldingPowerCore = false;      // Whether PowerCore button is currently held
    private bool isChargingHealingCore = false;   // Whether currently charging HealingCore
    private bool isChargingPowerCore = false;     // Whether currently charging PowerCore

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
        
        // HealingCore and PowerCore abilities (hold-to-charge)
        controls.Player.HealingCore.started += ctx => StartChargingHealingCore();
        controls.Player.HealingCore.canceled += ctx => CancelChargingHealingCore();
        controls.Player.PowerCore.started += ctx => StartChargingPowerCore();
        controls.Player.PowerCore.canceled += ctx => CancelChargingPowerCore();
        
        // Combat Style Menu (like Mega Man 11's weapon wheel)
        controls.Player.CombatStyleMenu.performed += ctx => ToggleCombatStyleMenu();
        
        // Combat Style Cycling (left/right triggers like Mega Man 11)
        controls.Player.CombatStyleLeft.performed += ctx => CycleCombatStylePrevious();
        controls.Player.CombatStyleRight.performed += ctx => CycleCombatStyleNext();
        
        // NOTE: Core Burst is activated by pressing HealingCore + PowerCore simultaneously
        // No separate input action needed - detected in Update() via combo
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
        
        // Update core ability charging (hold-to-charge mechanic)
        UpdateCoreAbilityCharging();
        
        // Check for Core Burst combo (HealingCore + PowerCore pressed simultaneously)
        // Only trigger if both are fully charged or being held together
        bool healingCorePressed = controls.Player.HealingCore.IsPressed();
        bool powerCorePressed = controls.Player.PowerCore.IsPressed();
        if (healingCorePressed && powerCorePressed && attackTimer <= 0f && !isAttacking)
        {
            TryCoreBurst();
        }
        
        // Move player horizontally (always allow movement, even while attacking)
        // This gives Mega Man/Gunvolt feel where you can shoot while moving
        MovePlayer();
        
        // Apply enhanced falling physics for snappier jump feel
        ApplyBetterJump();
        
        // Allow combat style cycling even when menu is closed (Mega Man 11 style)
        // This lets players quickly switch without opening the menu
        // The menu toggle is handled separately via button press
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
        // If your sprites all face left by default, flip when moving right
        if (moveInput.x != 0)
            sr.flipX = moveInput.x > 0;  // Flip horizontally when moving right (if sprites face left)
        
        // Update animation based on movement
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;  // Small threshold to avoid jitter
        
        // Always update the IsMoving bool - this ensures the Animator responds immediately
        // when movement stops, even if the state didn't technically "change"
        characterAnimator?.SetMoving(isMoving);
        wasMoving = isMoving;
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
            characterAnimator?.TriggerJump();
        }
    }

    // ============================================
    // COMBAT & ABILITIES (Light/Heavy/HealingCore/PowerCore/CoreBurst)
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
        
        // Trigger animation (automatically chooses ground or air variant)
        characterAnimator?.TriggerLightAttack();
        
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
        
        // Trigger animation (automatically chooses ground or air variant)
        characterAnimator?.TriggerHeavyAttack();
        
        // End attack state after animation duration (slightly longer than light)
        Invoke(nameof(EndAttack), 0.8f);
    }

    // ============================================
    // CORE ABILITY CHARGING (Hold-to-Charge System)
    // ============================================

    /// <summary>
    /// Starts charging HealingCore when button is pressed.
    /// </summary>
    void StartChargingHealingCore()
    {
        // Don't start charging if already attacking or on cooldown
        if (attackTimer > 0f || isAttacking || isChargingPowerCore)
            return;

        isHoldingHealingCore = true;
        isChargingHealingCore = true;
        healingCoreHoldTimer = 0f;
        
        Debug.Log("Started charging HealingCore...");
    }

    /// <summary>
    /// Cancels HealingCore charge if button is released early.
    /// </summary>
    void CancelChargingHealingCore()
    {
        if (isChargingHealingCore && healingCoreHoldTimer < healingCoreHoldDuration)
        {
            // Button released before full charge - cancel
            Debug.Log("HealingCore charge cancelled (released early)");
            ResetHealingCoreCharge();
        }
        
        isHoldingHealingCore = false;
    }

    /// <summary>
    /// Starts charging PowerCore when button is pressed.
    /// </summary>
    void StartChargingPowerCore()
    {
        // Don't start charging if already attacking or on cooldown
        if (attackTimer > 0f || isAttacking || isChargingHealingCore)
            return;

        isHoldingPowerCore = true;
        isChargingPowerCore = true;
        powerCoreHoldTimer = 0f;
        
        Debug.Log("Started charging PowerCore...");
    }

    /// <summary>
    /// Cancels PowerCore charge if button is released early.
    /// </summary>
    void CancelChargingPowerCore()
    {
        if (isChargingPowerCore && powerCoreHoldTimer < powerCoreHoldDuration)
        {
            // Button released before full charge - cancel
            Debug.Log("PowerCore charge cancelled (released early)");
            ResetPowerCoreCharge();
        }
        
        isHoldingPowerCore = false;
    }

    /// <summary>
    /// Updates the charging timers for both core abilities.
    /// Called every frame in Update().
    /// </summary>
    void UpdateCoreAbilityCharging()
    {
        // Update HealingCore charging
        if (isChargingHealingCore && isHoldingHealingCore)
        {
            healingCoreHoldTimer += Time.deltaTime;
            
            // Update animator with charging state and progress
            float progress = GetHealingCoreChargeProgress();
            characterAnimator?.SetHealingCoreCharging(true, progress);
            
            // Check if fully charged
            if (healingCoreHoldTimer >= healingCoreHoldDuration)
            {
                ActivateHealingCore();
            }
        }
        else
        {
            // Not charging - reset animator parameters
            characterAnimator?.SetHealingCoreCharging(false, 0f);
        }

        // Update PowerCore charging
        if (isChargingPowerCore && isHoldingPowerCore)
        {
            powerCoreHoldTimer += Time.deltaTime;
            
            // Update animator with charging state and progress
            float progress = GetPowerCoreChargeProgress();
            characterAnimator?.SetPowerCoreCharging(true, progress);
            
            // Check if fully charged
            if (powerCoreHoldTimer >= powerCoreHoldDuration)
            {
                ActivatePowerCore();
            }
        }
        else
        {
            // Not charging - reset animator parameters
            characterAnimator?.SetPowerCoreCharging(false, 0f);
        }
    }

    /// <summary>
    /// Activates HealingCore ability after full charge.
    /// </summary>
    void ActivateHealingCore()
    {
        if (attackTimer > 0f || isAttacking)
        {
            ResetHealingCoreCharge();
            return;
        }

        isAttacking = true;
        attackTimer = attackCooldown;

        // Reset charging state
        ResetHealingCoreCharge();

        // Trigger the ability
        characterAnimator?.TriggerHealingCore();
        Debug.Log("HealingCore activated!");

        // Duration can be tweaked based on your animation length
        Invoke(nameof(EndAttack), 0.7f);
    }

    /// <summary>
    /// Activates PowerCore ability after full charge.
    /// </summary>
    void ActivatePowerCore()
    {
        if (attackTimer > 0f || isAttacking)
        {
            ResetPowerCoreCharge();
            return;
        }

        isAttacking = true;
        attackTimer = attackCooldown;

        // Reset charging state
        ResetPowerCoreCharge();

        // Trigger the ability
        characterAnimator?.TriggerPowerCore();
        Debug.Log("PowerCore activated!");

        Invoke(nameof(EndAttack), 0.7f);
    }

    /// <summary>
    /// Resets HealingCore charging state.
    /// </summary>
    void ResetHealingCoreCharge()
    {
        isChargingHealingCore = false;
        isHoldingHealingCore = false;
        healingCoreHoldTimer = 0f;
    }

    /// <summary>
    /// Resets PowerCore charging state.
    /// </summary>
    void ResetPowerCoreCharge()
    {
        isChargingPowerCore = false;
        isHoldingPowerCore = false;
        powerCoreHoldTimer = 0f;
    }

    /// <summary>
    /// Gets the current HealingCore charge progress (0.0 to 1.0).
    /// Useful for UI progress bars or visual feedback.
    /// </summary>
    public float GetHealingCoreChargeProgress()
    {
        if (!isChargingHealingCore) return 0f;
        return Mathf.Clamp01(healingCoreHoldTimer / healingCoreHoldDuration);
    }

    /// <summary>
    /// Gets the current PowerCore charge progress (0.0 to 1.0).
    /// Useful for UI progress bars or visual feedback.
    /// </summary>
    public float GetPowerCoreChargeProgress()
    {
        if (!isChargingPowerCore) return 0f;
        return Mathf.Clamp01(powerCoreHoldTimer / powerCoreHoldDuration);
    }

    /// <summary>
    /// Core Burst ability (your "super" move).
    /// Activated by pressing HealingCore + PowerCore simultaneously.
    /// Animation only for now – damage/effects can be added later.
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
    // COMBAT STYLE MENU (Mega Man 11 style weapon wheel)
    // ============================================

    /// <summary>
    /// Toggles the combat style menu open/closed.
    /// Works like Mega Man 11's weapon select wheel.
    /// </summary>
    void ToggleCombatStyleMenu()
    {
        // Don't toggle if paused
        if (PauseMenu.isPaused) return;

        if (CombatStyleMenu.Instance != null)
        {
            CombatStyleMenu.Instance.ToggleMenu();
        }
        else
        {
            Debug.LogWarning("CombatStyleMenu.Instance not found! Make sure a CombatStyleMenu object exists in the scene.");
        }
    }

    /// <summary>
    /// Cycles to the previous combat style (like pressing left trigger in Mega Man 11).
    /// Works whether menu is open or closed.
    /// </summary>
    void CycleCombatStylePrevious()
    {
        // Don't cycle if paused
        if (PauseMenu.isPaused) return;

        // Check if GameManager exists before using it
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null. Cannot cycle combat style.");
            return;
        }

        GameManager.Instance.CycleToPreviousCombatStyle();
        
        // If menu is open, it should update to show the new selection
        // (You'll handle UI updates in the CombatStyleMenu script when you create the UI)
    }

    /// <summary>
    /// Cycles to the next combat style (like pressing right trigger in Mega Man 11).
    /// Works whether menu is open or closed.
    /// </summary>
    void CycleCombatStyleNext()
    {
        // Don't cycle if paused
        if (PauseMenu.isPaused) return;

        // Check if GameManager exists before using it
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null. Cannot cycle combat style.");
            return;
        }

        GameManager.Instance.CycleToNextCombatStyle();
        
        // If menu is open, it should update to show the new selection
        // (You'll handle UI updates in the CombatStyleMenu script when you create the UI)
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
