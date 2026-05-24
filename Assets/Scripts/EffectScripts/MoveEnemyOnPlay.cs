using UnityEngine;
using System.Collections;

public class MoveEnemyOnPlay : MonoBehaviour
{
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();

        Player opponent = GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player);
        int fieldID = GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField);
        // checking if there are units on the opposite field
        if (GameManager.instance.fieldManager.IsFieldEmpty(opponent.fields[fieldID])) yield break;

        yield return StartCoroutine(effect.ShowEffect());

        // getting the enemy unit
        Unit opposingUnit;
        for (int i = 0; i < opponent.fields[fieldID].units.Length; i++)
        {
            if (opponent.fields[fieldID].units[i] != null)
            {
                opposingUnit = opponent.fields[fieldID].units[i];

                // moving the unit
                bool leftAvailable = false;
                int leftFreeSlot = 0;
                // checking the left side
                if (fieldID - 1 >= 0)
                {
                    for (int a = 0; a < opponent.fields[fieldID - 1].units.Length; a++)
                    {
                        if (opponent.fields[fieldID - 1].units[a] == null) { leftFreeSlot = a; leftAvailable = true; break; }
                    }
                }

                bool rightAvailable = false;
                int rightFreeSlot = 0;
                // checking the right side
                if (fieldID + 1 < opponent.fields.Length)
                {
                    for (int a = 0; a < opponent.fields[fieldID + 1].units.Length; a++)
                    {
                        if (opponent.fields[fieldID + 1].units[a] == null) { rightFreeSlot = a; rightAvailable = true; break; }
                    }
                }

                // choosing which side to move to
                int chosenSide = 0; // left - 1; right +1
                if (leftAvailable && rightAvailable) chosenSide = (Random.value > 0.5f) ? -1 : +1;
                else chosenSide = (leftAvailable) ? -1 : +1;
                // choosing the slot
                int slotToMoveTo = (chosenSide == -1) ? leftFreeSlot : rightFreeSlot;
                // getting adjacent unit if there is any
                Unit adjacentUnit = GameManager.instance.fieldManager.GetAnotherUnit(opposingUnit);
                // moving the unit
                GameManager.instance.fieldManager.MoveUnit(opposingUnit, opponent.fields[fieldID + chosenSide], slotToMoveTo);
                // moving the back unit upfront
                if (adjacentUnit != null) GameManager.instance.fieldManager.MoveUnit(adjacentUnit, opponent.fields[fieldID], 0);

                break;
            }
        }

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
