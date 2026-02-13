using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;

public class BattleManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] PowerCounter playerPowerUI;
    [SerializeField] PowerCounter opponentPowerUI;

    [Header("rolling")]
    [SerializeField] GameObject dice;
    [SerializeField] Button diceButton;
    [SerializeField] Image diceSprite;
    bool rolled = false;
    int diceValue = 0;
    [SerializeField] Sprite[] diceFace = new Sprite[6];
    [SerializeField] float diceIntervals;
    [SerializeField] GameObject oppDice;
    [SerializeField] Image oppDiceSprite;
    [SerializeField] float opponentRollingTime;

    int currentLine;

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
 
        if (currentLine < GameManager.instance.player.fields.Length)
        {
            // Getting all the units battling on currentLine and initialize battle
            List<Unit> playerUnits = GetFieldUnits(GameManager.instance.player.fields[currentLine]);
            List<Unit> opponentUnits = GetFieldUnits(GameManager.instance.opponent.fields[currentLine]);
            if (playerUnits.Count > 0 || opponentUnits.Count > 0) StartCoroutine(InitBattleLine(playerUnits, opponentUnits));

            // If line is empty go to next line instead
            else { NextLine(); return; }
        }
        // Ending the battle phase after finished with last line
        else
        {
            GameManager.instance.player.endStateReady = true;
            GameManager.instance.opponent.endStateReady = true;
            GameManager.instance.EndTurn();
        }
    }

    List<Unit> GetAllLineUnits()
    {
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;
        Field field = player.fields[currentLine];
        Field oppField = opponent.fields[currentLine];
        List<Unit> unitsToReturn = new List<Unit>();

        unitsToReturn.AddRange(GetFieldUnits(field));
        unitsToReturn.AddRange(GetFieldUnits(oppField));

        return unitsToReturn;
    }

    List<Unit> GetFieldUnits(Field field)
    {
        List<Unit> unitsToReturn = new List<Unit>();

        for (int i = 0; i < field.units.Length; i++)
        {
            if (field.units[i] != null) unitsToReturn.Add(field.units[i]);
        }

        return unitsToReturn;
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

        // Opponent units first
        if (opponentUnits.Count > 0)
        { 
            foreach (Unit unit in opponentUnits)
            {
                if (unit.card.currentPower > 0) yield return StartCoroutine(opponentPowerUI.AddPower(unit.card.currentPower, unit));
            }
            // Opponent rolls
            StartCoroutine(opponentPowerUI.RollDicePower());
        }

        // Player units after
        if (playerUnits.Count > 0)
        {
            foreach (Unit unit in playerUnits)
            {
                if (unit.card.currentPower > 0) yield return StartCoroutine(playerPowerUI.AddPower(unit.card.currentPower, unit));
            }
            playerPowerUI.EnableDice();
        }
        // Wait for player(s) to roll dices
        while (!playerPowerUI.diceRolled && !opponentPowerUI.diceRolled)
        {
            yield return null;
        }


        yield return new WaitForSeconds(1f);
        // COMPARING POWER
        NextLine();//temp
    }   

    void ResolveLine(Player winner, Player loser)
    {

    }
}
