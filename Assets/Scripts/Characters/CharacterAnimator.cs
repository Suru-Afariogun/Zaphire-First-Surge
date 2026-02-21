using UnityEngine;

/// <summary>
/// Thin wrapper around Animator so the rest of the code can stay clean.
/// All combat / movement animation triggers live here.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator anim;

    // Track grounded state so attack methods can choose the right animation
    private bool isGrounded = false;

    /// <summary>
    /// Tells the animator whether the character is running horizontally.
    /// Matches the Mega Man 7 style spec: Animator uses a single bool "IsMoving"
    /// to switch between Idle and Run, with instant transitions.
    /// </summary>
    public void SetMoving(bool value)
    {
        if (anim == null) return;

        // Update the "IsMoving" bool – Animator transitions should be:
        // Idle  -> Run  when IsMoving == true
        // Run   -> Idle when IsMoving == false
        // Exit Time OFF, Transition Duration 0 (no smoothing / blending)
        anim.SetBool("IsMoving", value);
    }

    /// <summary>
    /// Updates grounded state for jump / fall animations.
    /// Also used internally to choose ground vs air attack animations.
    /// </summary>
    public void SetGrounded(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsGrounded", value);
        isGrounded = value;  // Store for attack methods
    }

    /// <summary>
    /// Triggers the jump animation.
    /// </summary>
    public void TriggerJump()
    {
        if (anim == null) return;
        anim.SetTrigger("Jump");
    }

    // -----------------------------
    // ATTACK / ABILITY TRIGGERS
    // -----------------------------

    /// <summary>
    /// Light attack - automatically uses ground or air animation based on current state.
    /// Same button, same attack, but animation differs based on whether you're grounded.
    /// </summary>
    public void TriggerLightAttack()
    {
        if (anim == null) return;
        // Choose animation based on grounded state
        if (isGrounded)
            anim.SetTrigger("LightAttackGround");
        else
            anim.SetTrigger("LightAttackAir");
    }

    /// <summary>
    /// Heavy attack - automatically uses ground or air animation based on current state.
    /// Same button, same attack, but animation differs based on whether you're grounded.
    /// </summary>
    public void TriggerHeavyAttack()
    {
        if (anim == null) return;
        // Choose animation based on grounded state
        if (isGrounded)
            anim.SetTrigger("HeavyAttackGround");
        else
            anim.SetTrigger("HeavyAttackAir");
    }

    // -----------------------------
    // SUPPORT ABILITIES (Single animation, works in air and on ground)
    // -----------------------------

    /// <summary>
    /// HealingCore ability - uses the same animation whether grounded or in air.
    /// </summary>
    public void TriggerHealingCore()
    {
        if (anim == null) return;
        anim.SetTrigger("HealingCoreActivated");
    }

    /// <summary>
    /// PowerCore ability - uses the same animation whether grounded or in air.
    /// </summary>
    public void TriggerPowerCore()
    {
        if (anim == null) return;
        anim.SetTrigger("PowerCoreActivated");
    }

    /// <summary>
    /// Core Burst ability - uses the same animation whether grounded or in air.
    /// </summary>
    public void TriggerCoreBurst()
    {
        if (anim == null) return;
        anim.SetTrigger("CoreBurst");
    }

    // -----------------------------
    // CORE ABILITY CHARGING PARAMETERS
    // -----------------------------

    /// <summary>
    /// Sets the HealingCore charging state and progress.
    /// Call this every frame while charging to update the animator.
    /// </summary>
    /// <param name="isCharging">Whether currently charging HealingCore</param>
    /// <param name="chargeProgress">Charge progress from 0.0 to 1.0</param>
    public void SetHealingCoreCharging(bool isCharging, float chargeProgress = 0f)
    {
        if (anim == null) return;
        anim.SetBool("IsChargingHealingCore", isCharging);
        anim.SetFloat("HealingCoreChargeProgress", Mathf.Clamp01(chargeProgress));
    }

    /// <summary>
    /// Sets the PowerCore charging state and progress.
    /// Call this every frame while charging to update the animator.
    /// </summary>
    /// <param name="isCharging">Whether currently charging PowerCore</param>
    /// <param name="chargeProgress">Charge progress from 0.0 to 1.0</param>
    public void SetPowerCoreCharging(bool isCharging, float chargeProgress = 0f)
    {
        if (anim == null) return;
        anim.SetBool("isChargingPowerCore", isCharging);
        anim.SetFloat("PowerCoreChargeProgress", Mathf.Clamp01(chargeProgress));
    }

    // Commented out for now - going for snappy Mega Man feel rather than smooth recovery animations
    // Uncomment if you want to add recovery animations later
    // public void TriggerRecover() => anim?.SetTrigger("Recover");
}