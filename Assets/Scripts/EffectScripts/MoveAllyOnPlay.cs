using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveAllyOnPlay : MonoBehaviour
{
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();

        Unit unitToMove = GameManager.instance.fieldManager.GetAnotherUnit(effect.unit);
        if (unitToMove == null) yield break;

        yield return StartCoroutine(effect.ShowEffect());

        // moving an ally
        // getting all empty slots on the field to the left
        int fieldID = GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField);
        int leftFieldID = fieldID - 1;
        if (leftFieldID >= 0)
        {
            Player player = effect.unit.card.player;
            for (int i = 0; i < player.fields[leftFieldID].units.Length; i++)
            {
                if (player.fields[leftFieldID].units[i] == null)
                {
                    GameManager.instance.fieldManager.MoveUnit(unitToMove, player.fields[leftFieldID], i);
                    GameManager.instance.fieldManager.MoveUnit(effect.unit, player.fields[fieldID], 0);
                    break;
                }
            }
        }
        yield return StartCoroutine(effect.StopShowEffect());
    }
}
