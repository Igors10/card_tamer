using System.Collections;
using UnityEngine;

/// <summary>
/// Default visuals and sounds played around every effect. Change juice here and every ability gets it
/// </summary>
public static class EffectPresenter
{
    public const float PauseBeforeEffect = 1f;
    public const float PauseAfterEffect = 1f;
    const float textFadeOutTime = 0.3f;

    public static IEnumerator Show(EffectContext ctx, string customText = null)
    {
        Unit unit = ctx.self;

        // play soundeffect
        AudioManager.instance.PlaySFX("EffectSFX", 0.75f);

        // the sticker lights up on its card (a neighbour's sticker in the row lights up there)
        if (StickerVisible(ctx)) ctx.ability.Highlight(true);

        // short text under the unit saying what triggered ("Attacking!"), the description if the effect has none
        unit.AppearAbove(true);
        unit.skillTextObj.SetActive(false); // turning it off first stops a fade out left from the previous effect on this unit
        unit.skillTextObj.SetActive(true);
        unit.skillText.text = (string.IsNullOrEmpty(customText) && ctx.ability != null) ? ctx.ability.GetDescription() : customText;

        // playing particle effect
        Animations.instance.PopAnim(unit.sprite.gameObject, 0.35f, 0.15f);
        unit.cardEffectVFX.gameObject.SetActive(true);
        unit.cardEffectVFX.Play();

        yield return new WaitForSeconds(PauseBeforeEffect);
    }

    public static IEnumerator Hide(EffectContext ctx)
    {
        yield return new WaitForSeconds(PauseAfterEffect);

        if (ctx.ability != null) ctx.ability.Highlight(false);

        // unit could have been removed during the effect
        Unit unit = ctx.self;
        if (unit == null) yield break;

        unit.cardEffectVFX.gameObject.SetActive(false);
        if (unit.skillTextObj.activeInHierarchy) unit.skillTextObj.GetComponent<AutoFade>().FadeOut(textFadeOutTime);
        unit.AppearAbove(false);
    }

    static bool StickerVisible(EffectContext ctx) => ctx.ability != null && ctx.ability.gameObject.activeInHierarchy;
}
