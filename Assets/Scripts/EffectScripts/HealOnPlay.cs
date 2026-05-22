using System.Collections;
using UnityEngine;

public class HealOnPlay : MonoBehaviour
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

        // healing
        Player player = effect.unit.card.player;
        player.health++;
        player.playerUI.Refresh();

        yield return StartCoroutine(effect.StopShowEffect());
    }
}
