using UnityEngine;
using System.Collections;
public class BuffSelfOnRoll : MonoBehaviour
{
    [SerializeField] int neededNumber = 2;
    [SerializeField] int buffAmount = 1;
    private void OnEnable()
    {
        // adding this to effect stack on play
        GameManager.OnDiceRolled += TriggerUseEffect;
    }

    private void OnDisable()
    {
        GameManager.OnDiceRolled -= TriggerUseEffect;
    }

    void TriggerUseEffect(Card cardRolled, int result)
    {
        if (result == neededNumber) GameManager.instance.executeManager.rollEffectStack.Add(UseEffect());
    }

    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // buffing itself
        effect.unit.card.GainPower(buffAmount);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
