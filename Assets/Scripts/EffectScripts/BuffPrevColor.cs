using UnityEngine;
using System.Collections;

public class BuffPrevColor : MonoBehaviour
{
    [SerializeField] int buffAmount = 3;
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // getting the correct list id
        int listID = Colors.instance.GetSecondaryColorID(effect.unit.card.abilities[0].abilityColor);

        // checking if list has any unit
        if (GameManager.instance.fieldManager.coloredUnitList[listID].Count < 1)
        {
            yield return StartCoroutine(effect.StopShowEffect());
            yield break;
        }
        // if last unit in the array is the owner of this effect skip it, buff next previous one
        else if (GameManager.instance.fieldManager.coloredUnitList[listID][GameManager.instance.fieldManager.coloredUnitList[listID].Count - 1] == effect.unit 
            && GameManager.instance.fieldManager.coloredUnitList[listID].Count > 1)
        {
            GameManager.instance.fieldManager.coloredUnitList[listID][GameManager.instance.fieldManager.coloredUnitList[listID].Count - 2].card.GainPower(buffAmount);
        }
        // buff the last spawned unit of given color
        else
        {
            GameManager.instance.fieldManager.coloredUnitList[listID][GameManager.instance.fieldManager.coloredUnitList[listID].Count - 1].card.GainPower(buffAmount);
        }

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
