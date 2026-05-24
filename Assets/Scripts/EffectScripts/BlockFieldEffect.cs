using UnityEngine;
using System.Collections;

public class BlockFieldEffect : MonoBehaviour
{
    [SerializeField] int blockRoundsAmount = 2;

    private void Start()
    {
        // adding this to effect stack on play
        GameManager.instance.executeManager.effectStack.Add(UseEffect());
    }

    IEnumerator UseEffect()
    {
        Effect effect = GetComponent<Effect>();
        yield return StartCoroutine(effect.ShowEffect());

        // blocking field
        int fieldID = GameManager.instance.fieldManager.GetFieldID(effect.unit.currentField);
        Player opponent = GameManager.instance.GetOpponentOfPlayer(effect.unit.card.player);
        opponent.fields[fieldID].roundsBlocked += 2;
        opponent.fields[fieldID].RefreshFieldVisuals();

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
