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

        

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
