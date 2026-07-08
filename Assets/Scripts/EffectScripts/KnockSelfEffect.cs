using UnityEngine;
using System.Collections;

public class KnockSelfEffect : MonoBehaviour
{
    [SerializeField] int buffAmount = 3;
    private void OnEnable()
    {
        // adding this to effect stack on line resolved
        GameManager.OnLineResolved += TriggerUseEffect;

        // adding the initial buff to on play stack
        GameManager.instance.executeManager.effectStack.Add(UseBuffEffect());
    }

    private void OnDisable()
    {
        GameManager.OnLineResolved -= TriggerUseEffect;
    }

    void TriggerUseEffect()
    {
        Effect effect = GetComponent<Effect>();
        bool correctLine = GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField) == GameManager.instance.battleManager.currentLine; 
        if (GetComponent<Effect>().unit.stunned != true && correctLine) GameManager.instance.executeManager.battleOverEffectStack.Add(UseEffect());
    }

    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect(1));

        // knocking itself out
        StartCoroutine(effect.unit.KnockOut());

        yield return StartCoroutine(effect.StopShowEffect());
    }

    IEnumerator UseBuffEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect(0));

        // buffing itself
        effect.unit.card.GainPower(buffAmount);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
