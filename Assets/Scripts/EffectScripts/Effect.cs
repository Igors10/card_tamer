using System.Collections;
using UnityEngine;


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

    private void Awake()
    {
        // getting reference to unit host
        unit = GetComponentInParent<Unit>();
    }

    public IEnumerator ShowEffect()
    {
        // showing text
        unit.skillText.gameObject.SetActive(true);
        unit.skillText.text = unit.card.abilities[0].abilityData.abilityDescription;

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
        unit.skillText.gameObject.SetActive(false);
    }
}
