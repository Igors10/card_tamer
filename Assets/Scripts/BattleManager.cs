using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using FishNet.Demo.AdditiveScenes;
using System.ComponentModel.Design;

public class BattleManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] PowerCounter playerPowerUI;
    [SerializeField] PowerCounter opponentPowerUI;
    public GameObject damageObj;
    [SerializeField] DeathAnim death;

    [Header("prefabs")]
    public GameObject dmgParticlePrefab;
    
    [Header("round end")]
    [SerializeField] GameObject knockedUnitsMessege;
    [SerializeField] float deathThinkingTime;
    [SerializeField] float deathChoosingTime;
    [SerializeField] float afterDeathTime;

    [HideInInspector] public int currentLine;

    public void ResetBattleVals()
    {
        currentLine = -1;
        
        ResetLineVals();
    }

    void ResetLineVals()
    {
        playerPowerUI.ResetCounter();
        opponentPowerUI.ResetCounter();

        playerPowerUI.gameObject.SetActive(false);
        opponentPowerUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// Switches to next battle line
    /// </summary>
    public void NextLine()
    {
        currentLine++;
 
        if (currentLine < GameManager.instance.player.fields.Length && GameManager.instance.gameOver != true)
        {
            // Getting all the units battling on currentLine and initialize battle
            List<Unit> playerUnits = GameManager.instance.player.fields[currentLine].GetFieldUnits();
            List<Unit> opponentUnits = GameManager.instance.opponent.fields[currentLine].GetFieldUnits();
            if (playerUnits.Count > 0 || opponentUnits.Count > 0) StartCoroutine(InitBattleLine(playerUnits, opponentUnits));

            // If line is empty go to next line instead
            else { NextLine(); return; }
        }
        // Ending the battle phase after finished with last line
        else
        {
            StartCoroutine(WrapUpBattle());
        }
    }

    /// <summary>
    /// Focuses camera and visuals on current battle line
    /// </summary>
    public IEnumerator InitBattleLine(List<Unit> playerUnits, List<Unit> opponentUnits)
    {
        // RESETTING VARIABLES
        ResetLineVals();

        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;
        Field field = player.fields[currentLine];
        Field oppField = opponent.fields[currentLine];

        // PREPARING THE LINE

        // Moving the camera
        Vector3 posBetweenFields = Vector3.Lerp(field.transform.position, oppField.transform.position, 0.5f);
        Vector3 targetPos = new Vector3(posBetweenFields.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
        yield return StartCoroutine(Camera.main.GetComponent<Viewpoint>().MoveCamera(targetPos, 0.6f));

        // Enabling power UI
        playerPowerUI.gameObject.SetActive(true);
        opponentPowerUI.gameObject.SetActive(true);

        // Fading all lines, except current line fields
        for (int i = 0; i < player.fields.Length; i++)
        {
            player.fields[i].FadeOut(i != currentLine);
            opponent.fields[i].FadeOut(i != currentLine);
        }

        // Coloring lines in respective player colors
        field.sprite.color = player.playerColor;
        oppField.sprite.color = opponent.playerColor;

        yield return new WaitForSeconds(1f);

        // ADDING UNIT POWER

        int playerPower = 0;
        int opponentPower = 0;
        // Opponent's power
        foreach (Unit unit in opponentUnits)
        {
            if (unit.card.currentPower > 0 && !unit.stunned)
            {
                StartCoroutine(opponentPowerUI.AddPower(unit.card.currentPower, unit));
                opponentPower += unit.card.currentPower;
            }
        }
        // Wait for power to get calculated
        while (opponentPowerUI.currentPower < opponentPower)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);


        // Player's power
        foreach (Unit unit in playerUnits)
        {
            if (unit.card.currentPower > 0 && !unit.stunned)
            {
                StartCoroutine(playerPowerUI.AddPower(unit.card.currentPower, unit));
                playerPower += unit.card.currentPower;
            }
        }
        // Wait for power to get calculated
        while (playerPowerUI.currentPower < playerPower)
        {
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);

        yield return new WaitForSeconds(1f);
        // COMPARING POWER
        if (playerPowerUI.currentPower > opponentPowerUI.currentPower) Debug.Log("BattleManager: player has more power");
        if (playerPowerUI.currentPower < opponentPowerUI.currentPower) Debug.Log("BattleManager: opponent has more power");
        if (playerPowerUI.currentPower == opponentPowerUI.currentPower) Debug.Log("BattleManager: it is tied for the power");
        playerPowerUI.ResolveCounter(playerPowerUI.currentPower >= opponentPowerUI.currentPower, new Field[] { field, oppField });
        opponentPowerUI.ResolveCounter(opponentPowerUI.currentPower >= playerPowerUI.currentPower, new Field[] { oppField, field });

        // Waiting for both power counters to get resolved
        while (!playerPowerUI.resolved || !opponentPowerUI.resolved) yield return null;

        // trigger any end of line effects
        GameManager.instance.BroadcastOnLineResolved();
        yield return GameManager.instance.executeManager.TriggerEffects("onBattleEnd");

        // Switching to next line
        NextLine();
    }

    IEnumerator WrapUpBattle()
    {
        // resetting battle visuals
        ResetLineVals();

        // Putting the camera where it was before battling phase
        Vector3 stateCameraPosition = GameManager.instance.GetState().cameraPosition;
        Vector3 centerCameraPosition = new Vector3(0, Camera.main.transform.position.y, Camera.main.transform.position.z);
        yield return StartCoroutine(Camera.main.GetComponent<Viewpoint>().MoveCamera(centerCameraPosition, 0.6f));

        // Message that round ends
        knockedUnitsMessege.SetActive(true);

        // DISCARDING STUNNED UNITS
        // ========================

        // getting all stunned units
        List<Unit> playerStunnedUnits = GameManager.instance.fieldManager.GetStunnedUnits(GameManager.instance.player);
        List<Unit> opponentStunnedUnits = GameManager.instance.fieldManager.GetStunnedUnits(GameManager.instance.opponent);

        // Enalbing death 
        death.gameObject.SetActive(true);
        yield return new WaitForSeconds(deathThinkingTime);

        yield return StartCoroutine(StunnedUnitsDiscard(playerStunnedUnits));
        yield return StartCoroutine(StunnedUnitsDiscard(opponentStunnedUnits));

        yield return new WaitForSeconds(afterDeathTime);
        knockedUnitsMessege.SetActive(false);
        death.gameObject.SetActive(false);

        // Ending the phase
        GameManager.instance.player.endStateReady = true;
        GameManager.instance.opponent.endStateReady = true;
        GameManager.instance.EndTurn();
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
            do { randomUnitID = Random.Range(0, units.Count); } while(idsChosen.Contains(randomUnitID));
            idsChosen.Add(randomUnitID);

            units[randomUnitID].card.DestroyCard();

            // making death point finger at the unit 
            StartCoroutine(death.PointAnim(deathChoosingTime / 2));
            yield return new WaitForSeconds(deathChoosingTime);
        }
    }
}
