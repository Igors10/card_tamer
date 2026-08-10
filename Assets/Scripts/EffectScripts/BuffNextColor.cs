using UnityEngine;
using System.Collections;

public class BuffNextColor : MonoBehaviour
{
    [SerializeField] int buffAmount;
    bool effectUsed = false;

    private void Start()
    {
        GameManager.OnCardPlayed += TriggerUseEffect;
    }

    private void OnDisable()
    {
        GameManager.OnCardPlayed -= TriggerUseEffect;
    }

    void TriggerUseEffect(Card cardPlayed)
    {
        Effect effect = GetComponent<Effect>();

        // triggering the effect when a card with a matching color is played
        if (cardPlayed.cardData.secondaryColor == effect.unit.card.abilities[0].abilityColor && effectUsed == false) GameManager.instance.executeManager.effectStack.Add(UseEffect(cardPlayed.unit));
    }

    IEnumerator UseEffect(Unit unit)
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // debuffing enemy
        unit.card.GainPower(buffAmount);
        effectUsed = true;

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
