using UnityEngine;

/// <summary>
/// Thin wrapper around Animator so the rest of the code can stay clean.
/// All combat / movement animation triggers live here.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Header("References")]
    public Animator anim;

    /// <summary>
    /// Tells the animator whether the character is moving horizontally.
    /// Also fires a one-shot "StartMoving" trigger when movement begins.
    /// </summary>
    public void SetMoving(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsMoving", value);
        // Only trigger StartMoving when starting to move (not when stopping)
        if (value) 
        {
            anim.SetTrigger("StartMoving");
        }
        // When stopping, the IsMoving bool handles transition back to idle.
    }

    /// <summary>
    /// Updates grounded state for jump / fall animations.
    /// </summary>
    public void SetGrounded(bool value)
    {
        if (anim == null) return;
        anim.SetBool("IsGrounded",value);
    }

    // -----------------------------
    // ATTACK / ABILITY TRIGGERS
    // -----------------------------

    // Light attack (was Attack A in older code)
    public void TriggerGroundLightAttack() => anim?.SetTrigger("AttackGroundA");
    public void TriggerAirLightAttack()    => anim?.SetTrigger("AttackAirA");

    // Heavy attack (was Attack B in older code)
    public void TriggerGroundHeavyAttack() => anim?.SetTrigger("AttackGroundB");
    public void TriggerAirHeavyAttack()    => anim?.SetTrigger("AttackAirB");

    // Support abilities – you'll create these triggers in the Animator:
    // "Heal", "Buff", "CoreBurst"
    public void TriggerHeal()              => anim?.SetTrigger("Heal");
    public void TriggerBuff()              => anim?.SetTrigger("Buff");
    public void TriggerCoreBurst()         => anim?.SetTrigger("CoreBurst");

    public void TriggerRecover()           => anim?.SetTrigger("Recover");
}