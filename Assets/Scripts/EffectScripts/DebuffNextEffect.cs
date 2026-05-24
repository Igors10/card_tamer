using UnityEngine;
using System.Collections;
public class DebuffNextEffect : MonoBehaviour
{
    [SerializeField] int debuffAmount;
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

        // triggering the effect when an opponent is spawning an enemy on the opposing field
        if (GameManager.instance.fieldManager.GetFieldID(cardPlayed.unit.currentField) == GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField)
            && cardPlayed.player != effect.unit.card.player && effectUsed == false) GameManager.instance.executeManager.effectStack.Add(UseEffect(cardPlayed.unit));
    }

    IEnumerator UseEffect(Unit enemyUnit)
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // debuffing enemy
        enemyUnit.card.GainPower(debuffAmount);
        effectUsed = true;

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
