using UnityEngine;
using System.Collections;

public class KnockOnRoll : MonoBehaviour
{
    [SerializeField] int neededNumber = 6;
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

        // knocking opposing enemies out
        int fieldID = GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField);
        Player opponent = GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player);

        for (int i = 0; i < opponent.fields[fieldID].units.Length; i++)
        {
            if (opponent.fields[fieldID].units[i] != null && opponent.fields[fieldID].units[i].stunned != true) 
            {
                yield return opponent.fields[fieldID].units[i].KnockOut(); break;
            }
        }

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
