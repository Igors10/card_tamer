using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class DiscardManager : MonoBehaviour
{
    [Header("animation times")]
    [SerializeField] float deathThinkingTime;
    [SerializeField] float afterDeathTime;
    [SerializeField] float deathChoosingTime;
    [SerializeField] float unitsMovingTime;

    [Header("refs")]
    [SerializeField] DeathAnim death;
    [SerializeField] Image[] units;
    [SerializeField] GameObject unitOffscreenPoint;

    private void Start()
    {
        // starting the unit discard
        GameManager.instance.discardManager = this;
    }

    public IEnumerator DiscardSequence()
    {
        yield return new WaitForSeconds(deathThinkingTime);

        // discard player units
        yield return StartCoroutine(UnitsDiscard(GameManager.instance.player));

        // discard enemy units
        yield return StartCoroutine(UnitsDiscard(GameManager.instance.opponent));

        yield return new WaitForSeconds(afterDeathTime);

        // Ending the phase
        GameManager.instance.player.endStateReady = true;
        GameManager.instance.opponent.endStateReady = true;
        GameManager.instance.EndTurn();
    }

    /// <summary>
    /// Makes discarded units of chosen player appear on the discard UI
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    IEnumerator UnitsDiscard(Player player)
    {
        // if no units lost then skip discard for this player
        if (player.cardsInDiscard.Count < 1) yield break;

        List<Vector3> unitDiscardPos = new List<Vector3>();
        List<Image> currentUnits = new List<Image>();

        // INITIALYZING UNITS
        for (int i = 0; i < player.cardsInDiscard.Count; i++)
        {
            // activating units
            units[i].gameObject.SetActive(true);
            units[i].sprite = player.cardsInDiscard[i].cardData.unitSprite;
            units[i].color = player.playerColor;
            currentUnits.Add(units[i]);

            // assigning units to a position
            Vector3 newUnitPosition = units[i].transform.position + new Vector3(Random.Range(-40, 40), Random.Range(-40, 40), 0f);
            unitDiscardPos.Add(newUnitPosition);
        }

        // MOVING UNITS IN
        // all units start off screen
        foreach (Image unit in currentUnits)
        {
            unit.gameObject.transform.position = unitOffscreenPoint.transform.position;
        }
        float t = 0;

        // moving units from off screen to their positions
        while (t < unitsMovingTime)
        {
            t += Time.deltaTime;
            float clampedT = t / unitsMovingTime;
            float coolT = t * t;

            for (int i = 0; i < currentUnits.Count; i++)
            {
                currentUnits[i].transform.position = Vector3.Lerp(unitOffscreenPoint.transform.position, unitDiscardPos[i], coolT);
            }

            yield return null;
        }

        // Death does its thing
        yield return StartCoroutine(DiscardLottery(currentUnits, player));

        // MAKING UNITS DISAPPEAR
        foreach (Image unit in currentUnits) unit.gameObject.SetActive(false);

    }

    /// <summary>
        /// Randomly chooses half of the stunned units and removes them from the game
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
    IEnumerator DiscardLottery(List<Image> units, Player player)
    {
        int discardUnitsAmount = (units.Count + 1) / 2; // returns half of the unit amount rounded up
        List<int> idsChosen = new List<int>();

        for (int i = 0; i < discardUnitsAmount; i++)
        {
            // chosing random unit that hasn't been chosen before
            int randomUnitID = 0;
            do { randomUnitID = Random.Range(0, units.Count); } while (idsChosen.Contains(randomUnitID));
            idsChosen.Add(randomUnitID);

            DiscardUnit(units[randomUnitID], i, player);

            // making death point finger at the unit 
            StartCoroutine(death.PointAnim(deathChoosingTime / 2));
            yield return new WaitForSeconds(deathChoosingTime);
        }
    }

    void DiscardUnit(Image unit, int unitID, Player player)
    {
        // dead unit visuals
        unit.color = new Color(Color.gray.r, Color.gray.g, Color.gray.b, 0.65f);
        ParticleManager.instance.SpawnVFX(unit.transform.position, "HitVFX");

        // removing the card
        player.cardsInDiscard[unitID].DestroyCard();
    }
}
