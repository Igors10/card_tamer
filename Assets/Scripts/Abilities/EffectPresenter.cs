using System.Collections;
using UnityEngine;

/// <summary>
/// Default visuals and sounds played around every effect. Change juice here and every ability gets it
/// </summary>
public static class EffectPresenter
{
    public const float PauseBeforeEffect = 1f;
    public const float PauseAfterEffect = 1f;
    public static IEnumerator Show(EffectContext ctx, string customText = null)
    {
        Unit unit = ctx.self;

        // the notifier shows whose sticker it is and what it does first (it plays the effect sound),
        // effects without a sticker (Momentum) only play the sound
        if (ctx.ability != null) yield return GameManager.instance.managerUI.effectNotifier.Show(ctx.ability, customText);
        else AudioManager.instance.PlaySFX("EffectSFX", 0.75f);

        unit.AppearAbove(true);

        // playing particle effect
        Animations.instance.PopAnim(unit.sprite.gameObject, 0.35f, 0.15f);
        unit.cardEffectVFX.gameObject.SetActive(true);
        unit.cardEffectVFX.Play();

        yield return new WaitForSeconds(PauseBeforeEffect);
    }

    public static IEnumerator Hide(EffectContext ctx)
    {
        yield return new WaitForSeconds(PauseAfterEffect);

        if (ctx.ability != null) GameManager.instance.managerUI.effectNotifier.Hide();

        // unit could have been removed during the effect
        Unit unit = ctx.self;
        if (unit == null) yield break;

        unit.cardEffectVFX.gameObject.SetActive(false);
        unit.AppearAbove(false);
    }
}
