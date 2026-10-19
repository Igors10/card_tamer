using System.Collections;
using UnityEngine;

public enum EffectTrigger
{
    OnPlay,             // when the card with this ability is played
    OnAnyCardPlayed,    // whenever any card is played (including this one) while this unit is on the board
    OnDiceRolled,       // whenever any unit finishes rolling a die
    OnOwnLineResolved,  // after the battle on this unit's line is resolved
    AfterAnyCardPlayed, // after any card is played and its own OnPlay effects are resolved, so its starting power is final (base or rolled)
    OnWouldBeKnockedOut // right before this unit gets knocked out
}

/// <summary>
/// Base class for every ability effect. Effects live inside AbilityObj (effects list) and are picked from a dropdown in the inspector.
/// To make a new effect: inherit from this, add [System.Serializable], override Execute (and CanTrigger if needed).
/// </summary>
[System.Serializable]
public abstract class AbilityEffect
{
    [Tooltip("When this effect triggers")]
    public EffectTrigger trigger = EffectTrigger.OnPlay;
    [Tooltip("Text shown above the unit when the effect triggers. Leave empty to show the ability description")]
    public string customText;
    [Tooltip("Plays the default effect presentation (sound, text, pop, VFX, pauses)")]
    public bool showPresentation = true;
    [Tooltip("Effect can trigger only once per round for each unit")]
    public bool oncePerRound;

    /// <summary>
    /// Checked before the effect is queued. Return false to skip the effect (no presentation is shown)
    /// </summary>
    public virtual bool CanTrigger(EffectContext ctx) => true;

    /// <summary>
    /// Only matters for OnWouldBeKnockedOut effects. Return true to stop the unit from getting knocked out
    /// </summary>
    public virtual bool PreventsKnockOut => false;

    /// <summary>
    /// The actual effect. Runs between the presentation's Show and Hide
    /// </summary>
    protected abstract IEnumerator Execute(EffectContext ctx);

    /// <summary>
    /// Runs the effect with its presentation, called by AbilityRunner through the ExecuteManager effect stacks
    /// </summary>
    public IEnumerator Run(EffectContext ctx)
    {
        // unit could have been removed between queueing and running
        if (ctx.self == null) yield break;

        if (showPresentation) yield return EffectPresenter.Show(ctx, customText);
        yield return Execute(ctx);
        // always hiding, even if Execute stopped early, so the text and VFX never get stuck
        if (showPresentation) yield return EffectPresenter.Hide(ctx);
    }
}
