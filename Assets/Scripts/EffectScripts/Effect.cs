using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public enum TriggerTime
{
    ON_PLAY,
    ON_ROUND_END,
    ON_ROLL
}
public class Effect : MonoBehaviour
{
    [SerializeField] float pauseBeforeEffect;
    [SerializeField] float pauseAfterEffect;
    [SerializeField] MonoBehaviour effectObj;
    [HideInInspector] public Unit unit;

    [Header("ability text")]
    [SerializeField] List<string> abilityText = new List<string>();
    private void Awake()
    {
        // getting reference to unit host
        unit = GetComponentInParent<Unit>();
    }

    public IEnumerator ShowEffect(int abilityTextNr = 0)
    {
        // play soundeffect
        AudioManager.instance.PlaySFX("EffectSFX");

        // making the unit appear in front so that the effect is clearly visible
        unit.AppearAbove(true);

        // showing text
        unit.skillTextObj.gameObject.SetActive(true);
        string textToDisplay = (abilityText.Count < 1) ? unit.card.abilities[0].abilityData.abilityDescription : abilityText[abilityTextNr];
        unit.skillText.text = textToDisplay;

        // playing particle effect
        Animations.instance.PopAnim(unit.sprite.gameObject, 0.35f, 0.15f);
        unit.cardEffectVFX.gameObject.SetActive(true);
        unit.cardEffectVFX.Play();

        yield return new WaitForSeconds(pauseBeforeEffect);
    }

    public IEnumerator StopShowEffect()
    {
        yield return new WaitForSeconds(pauseAfterEffect);
        unit.cardEffectVFX.gameObject.SetActive(false);
        unit.skillTextObj.gameObject.SetActive(false);
        unit.AppearAbove(false);
    }
}
