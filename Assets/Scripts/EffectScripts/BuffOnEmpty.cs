using UnityEngine;
using System.Collections;
public class BuffOnEmpty : MonoBehaviour
{
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // buffing if no other units on line


        yield return StartCoroutine(effect.StopShowEffect());
    }
}
