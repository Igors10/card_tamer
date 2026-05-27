using UnityEngine;
using System.Collections;

public class BuffOnPlayEffect : MonoBehaviour
{
    [SerializeField] int buffAmount = 1;
    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }
    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // buffing itself
        effect.unit.card.GainPower(buffAmount);

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
