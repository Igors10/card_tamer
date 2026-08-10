using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuffAllColor : MonoBehaviour
{
    [SerializeField] int buffAmount = 2;
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // buffing all units of chosen color
        List<Unit> unitList = GameManager.instance.fieldManager.coloredUnitList[Colors.instance.GetSecondaryColorID(effect.unit.card.abilities[0].abilityColor)];
        for (int i = 0; i < unitList.Count; i++)
        {
            if (unitList[i] != effect.unit) unitList[i].card.GainPower(buffAmount);
        }

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
