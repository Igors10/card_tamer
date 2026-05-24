using UnityEngine;
using System.Collections;
using Unity.VisualScripting;
public class BuffOnEmpty : MonoBehaviour
{
    [SerializeField] int baseBuffAmount = 2;
    bool buffApplied = false;

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
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }

    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();

        // buffing if no other units on the field
        Player player = effect.unit.card.player;
        bool fieldEmpty = GameManager.instance.fieldManager.IsFieldEmpty(effect.unit.currentField, effect.unit);

        if (fieldEmpty == buffApplied) yield break;

        yield return StartCoroutine(effect.ShowEffect());

        int buffAmount = (fieldEmpty) ? baseBuffAmount : -baseBuffAmount;
        effect.unit.card.GainPower(buffAmount);
        buffApplied = fieldEmpty;

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
