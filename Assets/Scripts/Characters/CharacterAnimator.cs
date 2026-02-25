using UnityEngine;

/// <summary>
/// Thin wrapper around Animator so the rest of the code can stay clean.
/// All combat / movement animation triggers live here.
/// Uses bools (IsInAir, IsAttacking) so the Animator can cleanly transition back to Idle
/// instead of getting stuck in Jump or Attack states.
///
/// ANIMATOR SETUP (in the Animator window):
/// - Parameters: IsMoving (bool), IsGrounded (bool), IsInAir (bool), IsAttacking (bool), IsDashing (bool), IsSprinting (bool),
///   IsChargingHealingCore (bool), HealingCoreChargeProgress (float),
///   IsChargingPowerCore (bool), PowerCoreChargeProgress (float),
///   Jump (trigger), LightAttackGround/Air, HeavyAttackGround/Air, HealingCoreActivated (trigger),
///   PowerCoreActivated (trigger), CoreBurst (trigger), Dash (trigger), etc.
/// - Run -&gt; Idle: condition IsMoving == false, Transition Duration 0, uncheck Exit Time.
/// - Jump -&gt; Idle: condition IsInAir == false, Transition Duration 0, uncheck Exit Time.
/// - Any Attack state -&gt; Idle: condition IsAttacking == false, Transition Duration 0, uncheck Exit Time.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator anim;

    // Track grounded state so attack methods can choose the right animation
    private bool isGrounded = false;

    /// <summary>Only set a bool if the Animator has that parameter (avoids errors when boss uses a different controller).</summary>
    static bool SetBoolSafe(Animator a, string name, bool value)
    {
        if (a == null) return false;
        for (int i = 0; i < a.parameterCount; i++)
        {
            var p = a.GetParameter(i);
            if (p.name == name && p.type == AnimatorControllerParameterType.Bool)
            {
                a.SetBool(name, value);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Tells the animator whether the character is running horizontally.
    /// Animator: Idle and Run use bool "IsMoving". Use transition conditions only (no Exit Time),
    /// and transition duration 0, so stopping input immediately returns to Idle.
    /// </summary>
    public void SetMoving(bool value)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsMoving", value);
    }

    /// <summary>
    /// Updates grounded state for jump/fall and clears IsInAir when we land so Jump state can exit.
    /// Also used internally to choose ground vs air attack animations.
    /// </summary>
    public void SetGrounded(bool value)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsGrounded", value);
        isGrounded = value;
        if (value)
            SetBoolSafe(anim, "IsInAir", false);
    }

    /// <summary>
    /// Triggers the jump animation and sets IsInAir so we can exit Jump when we land.
    /// Animator: add transition Jump -> Idle when IsInAir == false (no Exit Time).
    /// </summary>
    public void TriggerJump()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsInAir", true);
        SetTriggerSafe(anim, "Jump");
    }

    /// <summary>
    /// Triggers the dash animation. Animator should have a trigger parameter named \"Dash\"
    /// and a transition from Any State (or appropriate state) to Dash when this trigger is set.
    /// Also sets IsDashing so the Animator can stay in Dash state until the dash finishes.
    /// </summary>
    static bool SetTriggerSafe(Animator a, string name)
    {
        if (a == null) return false;
        for (int i = 0; i < a.parameterCount; i++)
        {
            var p = a.GetParameter(i);
            if (p.name == name && p.type == AnimatorControllerParameterType.Trigger)
            {
                a.SetTrigger(name);
                return true;
            }
        }
        return false;
    }

    public void TriggerDash()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsDashing", true);
        SetTriggerSafe(anim, "Dash");
    }

    /// <summary>
    /// Sets the IsDashing bool. PlayerController calls this when the dash starts and ends so
    /// the Animator can hold Dash state while dashing and exit cleanly when finished.
    /// </summary>
    public void SetDashing(bool value)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsDashing", value);
    }

    /// <summary>
    /// Sets the IsSprinting bool. Use this for a separate sprint animation when the player
    /// is holding movement + dash together (faster running animation).
    /// </summary>
    public void SetSprinting(bool value)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsSprinting", value);
    }

    /// <summary>
    /// Call when an attack/ability finishes so the Animator can leave Attack state.
    /// PlayerController calls this from EndAttack(). Animator: add transition Attack -> Idle when IsAttacking == false.
    /// </summary>
    public void SetAttackActive(bool value)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", value);
    }

    // -----------------------------
    // ATTACK / ABILITY TRIGGERS
    // -----------------------------

    /// <summary>
    /// Light attack - uses ground or air animation and sets IsAttacking so we can exit when EndAttack runs.
    /// </summary>
    public void TriggerLightAttack()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", true);
        if (isGrounded)
            SetTriggerSafe(anim, "LightAttackGround");
        else
            SetTriggerSafe(anim, "LightAttackAir");
    }

    /// <summary>
    /// Heavy attack - uses ground or air animation and sets IsAttacking so we can exit when EndAttack runs.
    /// </summary>
    public void TriggerHeavyAttack()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", true);
        if (isGrounded)
            SetTriggerSafe(anim, "HeavyAttackGround");
        else
            SetTriggerSafe(anim, "HeavyAttackAir");
    }

    // -----------------------------
    // SUPPORT ABILITIES (Single animation, works in air and on ground)
    // -----------------------------

    /// <summary>
    /// HealingCore ability - sets IsAttacking so we can exit when EndAttack runs.
    /// </summary>
    public void TriggerHealingCore()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", true);
        SetTriggerSafe(anim, "HealingCoreActivated");
    }

    /// <summary>
    /// PowerCore ability - sets IsAttacking so we can exit when EndAttack runs.
    /// </summary>
    public void TriggerPowerCore()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", true);
        SetTriggerSafe(anim, "PowerCoreActivated");
    }

    /// <summary>
    /// Core Burst ability - sets IsAttacking so we can exit when EndAttack runs.
    /// </summary>
    public void TriggerCoreBurst()
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsAttacking", true);
        SetTriggerSafe(anim, "CoreBurst");
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
        SetBoolSafe(anim, "IsChargingHealingCore", isCharging);
        SetFloatSafe(anim, "HealingCoreChargeProgress", Mathf.Clamp01(chargeProgress));
    }

    static bool SetFloatSafe(Animator a, string name, float value)
    {
        if (a == null) return false;
        for (int i = 0; i < a.parameterCount; i++)
        {
            var p = a.GetParameter(i);
            if (p.name == name && p.type == AnimatorControllerParameterType.Float)
            {
                a.SetFloat(name, value);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets the PowerCore charging state and progress.
    /// Call this every frame while charging to update the animator.
    /// Animator parameter name: <b>IsChargingPowerCore</b> (bool) and PowerCoreChargeProgress (float).
    /// </summary>
    /// <param name="isCharging">Whether currently charging PowerCore</param>
    /// <param name="chargeProgress">Charge progress from 0.0 to 1.0</param>
    public void SetPowerCoreCharging(bool isCharging, float chargeProgress = 0f)
    {
        if (anim == null) return;
        SetBoolSafe(anim, "IsChargingPowerCore", isCharging);
        SetFloatSafe(anim, "PowerCoreChargeProgress", Mathf.Clamp01(chargeProgress));
    }

        // Commented out for now - going for snappy, instant recovery rather than smooth recovery animations
    // Uncomment if you want to add recovery animations later
    // public void TriggerRecover() => anim?.SetTrigger("Recover");
}