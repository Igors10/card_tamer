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
        AudioManager.instance.PlaySFX("EffectSFX");

        // making the unit appear in front so that the effect is clearly visible
        unit.AppearAbove(true);

        // showing text
        unit.skillTextObj.SetActive(true);
        unit.skillText.text = string.IsNullOrEmpty(customText) ? ctx.ability.abilityData.GetAbilityDesc(ctx.ability.abilityColor) : customText;

        // playing particle effect
        Animations.instance.PopAnim(unit.sprite.gameObject, 0.35f, 0.15f);
        unit.cardEffectVFX.gameObject.SetActive(true);
        unit.cardEffectVFX.Play();

        yield return new WaitForSeconds(PauseBeforeEffect);
    }

    public static IEnumerator Hide(EffectContext ctx)
    {
        yield return new WaitForSeconds(PauseAfterEffect);

        // unit could have been removed during the effect
        Unit unit = ctx.self;
        if (unit == null) yield break;

        unit.cardEffectVFX.gameObject.SetActive(false);
        unit.skillTextObj.GetComponent<AutoFade>().FadeOut(textFadeOutTime);
        unit.AppearAbove(false);
    }
}
