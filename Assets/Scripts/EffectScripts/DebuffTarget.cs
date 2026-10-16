using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebuffTarget : MonoBehaviour
{
    [SerializeField] int debuffAmount = 2;
    [SerializeField] string targetDesc;
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // getting the target
        Player opponent = GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player);
        List<Unit> targetUnitList = GameManager.instance.fieldManager.GetAllUnits(false, opponent);
       
        yield return GameManager.instance.executeManager.ShowTargets(targetUnitList, targetDesc);

        // after a target is chosen
        Unit target = GameManager.instance.executeManager.GetTarget();
        if (target != null) target.card.GainPower(debuffAmount);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
