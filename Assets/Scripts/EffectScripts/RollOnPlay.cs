using UnityEngine;
using System.Collections;

public class RollOnPlay : MonoBehaviour
{
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect(0));

        // rolling the die
        yield return effect.unit.RollPower();

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
