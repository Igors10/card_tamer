using UnityEngine;
using System.Collections;

public class DebuffOnRoll : MonoBehaviour
{
    [SerializeField] int neededNumber = 4;
    [SerializeField] int debuffAmount = 2;
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
        Effect effect = GetComponent<Effect>();
        if (result == neededNumber && cardRolled.player == GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player)) GameManager.instance.executeManager.rollEffectStack.Add(UseEffect(cardRolled));
    }

    IEnumerator UseEffect(Card cardRolled)
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // debuffing whoever rolled a 4
        cardRolled.GainPower(debuffAmount);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
