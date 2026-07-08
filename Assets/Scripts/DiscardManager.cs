using FishNet.Demo.AdditiveScenes;
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

    List<Image> currentUnits = new List<Image>();
    int unitsDiscarded = 0;
    int unitsToDiscard = 0;
    [HideInInspector] public bool discardAvailable = false;

    private void Start()
    {
        // starting the unit discard
        GameManager.instance.discardManager = this;
    }

    public IEnumerator DiscardSequence(Player player)
    {
        // discard player units
        yield return StartCoroutine(UnitsDiscard(player));
        UpdateDiscardHint();
    }

    /// <summary>
    /// Makes discarded units of chosen player appear on the discard UI
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    IEnumerator UnitsDiscard(Player player)
    {
        // if no units lost then skip discard for this player
        if (player.cardsInDiscard.Count < 1)
        {
            yield return new WaitForSeconds(1f);
            GameManager.instance.managerUI.StateChangeMessage("Damn, no units lost by " + player.playerName);
            while (GameManager.instance.managerUI.stateTransitionObj.activeSelf) yield return null;

            StartCoroutine(FinishDiscard());
            yield break;
        }            

        // making the discard available
        discardAvailable = true;
        UpdateDiscardHint();

        // INITIALYZING UNITS
        List<Vector3> unitDiscardPos = new List<Vector3>();

        for (int i = 0; i < player.cardsInDiscard.Count; i++)
        {
            // activating units
            units[i].gameObject.SetActive(true);
            units[i].sprite = player.cardsInDiscard[i].cardData.unitSprite;
            units[i].color = player.playerColor;
            units[i].GetComponent<UnitAtDiscard>().storedCard = player.cardsInDiscard[i];
            units[i].GetComponent<UnitAtDiscard>().Discard(false);
            currentUnits.Add(units[i]);

            // assigning units to a position
            Vector3 newUnitPosition = units[i].transform.position + new Vector3(Random.Range(-40, 40), Random.Range(-40, 40), 0f);
            unitDiscardPos.Add(newUnitPosition);
        }

        // calculating how many units needs to be discarded
        unitsToDiscard = (currentUnits.Count + 1) / 2;
        UpdateDiscardHint();

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
    }

    // Checks if half of the units were discarded
    public void FinishDiscardCheck()
    {
        if (unitsDiscarded == unitsToDiscard)
        {
            GameManager.instance.managerUI.StateChangeMessage(unitsDiscarded + " units were discarded");
            

            StartCoroutine(FinishDiscard());
        }
    }

    IEnumerator FinishDiscard()
    {
        // notifying that all discards are made
        GameManager.instance.managerUI.NewHint("Unit discard complete!");
        while (GameManager.instance.managerUI.stateTransitionObj.activeSelf) yield return null;
        yield return new WaitForSeconds(afterDeathTime);

        // MAKING UNITS DISAPPEAR
        foreach (Image unit in currentUnits) unit.gameObject.SetActive(false);

        // resetting discard vals
        currentUnits.Clear();
        unitsDiscarded = 0;
        unitsToDiscard = 0;
        discardAvailable = false;

        // finishing the turn
        Player player = GameManager.instance.GetCurrentPlayer();
        player.endStateReady = true;
        player.EndTurn();
    }

    /// <summary>
        /// Lets the game choose discarded units for the player;
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
    public IEnumerator AutoDiscard(Player player)
    {
        Debug.Log("DiscardManager: auto discard started");

        int discardUnitsAmount = (currentUnits.Count + 1) / 2; // returns half of the unit amount rounded up
        List<int> idsChosen = new List<int>();

        for (int i = 0; i < discardUnitsAmount; i++)
        {
            // chosing random unit that hasn't been chosen before
            int randomUnitID = 0;
            do { randomUnitID = Random.Range(0, currentUnits.Count); } while (idsChosen.Contains(randomUnitID));
            idsChosen.Add(randomUnitID);

            DiscardUnit(currentUnits[randomUnitID].GetComponent<UnitAtDiscard>());
            yield return new WaitForSeconds(deathChoosingTime);
        }
    }

    void UpdateDiscardHint()
    {
        string newHintMessage = (unitsToDiscard == 0) ? GameManager.instance.GetState().defaultHintText : GameManager.instance.GetState().defaultHintText + " (" + unitsDiscarded + "/" + unitsToDiscard + " units discarded)";
        GameManager.instance.managerUI.NewHint(newHintMessage);
    }

    public void DiscardUnit(UnitAtDiscard unit)
    {
        // checking to not discard more units than necessary
        if (unitsDiscarded == unitsToDiscard) return;

        // updating discarded units amount
        unitsDiscarded++;
        Debug.Log("DiscardManager: unit discarded");
        UpdateDiscardHint();

        // dead unit animation
        unit.Discard(true);

        // making death point finger at the unit 
        StartCoroutine(death.PointAnim(deathChoosingTime / 2));

        // removing the card
        unit.storedCard.DestroyCard();

        // check if half was disacrded
        FinishDiscardCheck();
    }
}
