using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscardManager : MonoBehaviour
{
    [Header("animation times")]
    [SerializeField] float deathThinkingTime;
    [SerializeField] float afterDeathTime;
    [SerializeField] float deathChoosingTime;

    [Header("refs")]
    [SerializeField] DeathAnim death; 

    IEnumerator DiscardingCards()
    {
        yield return new WaitForSeconds(deathThinkingTime);

        //yield return StartCoroutine(StunnedUnitsDiscard(playerStunnedUnits));
        //yield return StartCoroutine(StunnedUnitsDiscard(opponentStunnedUnits));

        yield return new WaitForSeconds(afterDeathTime);
    }

    /// <summary>
    /// Randomly chooses half of the stunned units and removes them from the game
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    IEnumerator StunnedUnitsDiscard(List<Unit> units)
    {
        if (units.Count < 1) yield break;

        Player player = units[0].card.player;
        int discardUnitsAmount = (units.Count + 1) / 2; // returns half of the unit amount rounded up
        List<int> idsChosen = new List<int>();

        for (int i = 0; i < discardUnitsAmount; i++)
        {
            // chosing random unit that hasn't been chosen before
            int randomUnitID = 0;
            do { randomUnitID = Random.Range(0, units.Count); } while (idsChosen.Contains(randomUnitID));
            idsChosen.Add(randomUnitID);

            units[randomUnitID].card.DestroyCard();

            // making death point finger at the unit 
            StartCoroutine(death.PointAnim(deathChoosingTime / 2));
            yield return new WaitForSeconds(deathChoosingTime);
        }
    }
}
