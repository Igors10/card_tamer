using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;

public class BattleManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI playerPowerText;
    [SerializeField] TextMeshProUGUI opponentPowerText;

    [Header("Adding power")]
    [SerializeField] float unitScaleMod;
    [SerializeField] float textScaleMod;
    [SerializeField] float timePerUnit;

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
    bool oppRolled;

    int currentLine;

    public void ResetBattleVals()
    {
        currentLine = -1;
        playerPowerText.color = GameManager.instance.player.playerColor;
        opponentPowerText.color = GameManager.instance.opponent.playerColor;
        ResetLineVals();
    }

    void ResetLineVals()
    {
        GameManager.instance.player.currentLinePower = 0;
        GameManager.instance.opponent.currentLinePower = 0;
        playerPowerText.text = "0";
        opponentPowerText.text = "0";
        dice.SetActive(false);
        oppDice.SetActive(false);
        diceButton.interactable = true;
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
            List<Unit> units = GetAllLineUnits();
            if (units.Count > 0) StartCoroutine(InitBattleLine(units));

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

        for (int i = 0; i < field.units.Length; i++)
        {
            if (field.units[i] != null) unitsToReturn.Add(field.units[i]);
        }
        for (int i = 0; i < oppField.units.Length; i++)
        {
            if (oppField.units[i] != null) unitsToReturn.Add(oppField.units[i]);
        }

        return unitsToReturn;
    }

    /// <summary>
    /// Focuses camera and visuals on current battle line
    /// </summary>
    public IEnumerator InitBattleLine(List<Unit> units)
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
        
        foreach (Unit unit in units)
        {
            if (unit.card.currentPower > 0) yield return StartCoroutine(AddPower(unit.card.player, unit.card.currentPower, unit));
        }

        // ENABLING ROLL

        // Rolling for opponent if it's AI
        if (opponent.isAI) StartCoroutine(RollOpponetDice());

        GameManager.instance.readyButton.gameObject.SetActive(true);
        dice.SetActive(true);

        while (!rolled) // rolling dice until button is pressed 
        {
            diceValue = Random.Range(1, 7);
            diceSprite.sprite = diceFace[diceValue - 1];
            yield return new WaitForSeconds(diceIntervals);
        }
        rolled = false;

        // Adding power that was on the dice when it stopped
        yield return StartCoroutine(AddPower(player, diceValue));

        // COMPARING POWER
    }   

    public void RollPower()
    {
        rolled = true;
        diceButton.interactable = false;
    }

    IEnumerator RollOpponetDice(int diceValue = 0)
    {
        yield return new WaitForSeconds(1.5f);

        int opponentDiceValue = (diceValue == 0) ? Random.Range(0, 7) : diceValue;
        oppDice.SetActive(true);
        oppDiceSprite.sprite = diceFace[opponentDiceValue - 1];

        StartCoroutine(AddPower(GameManager.instance.opponent, opponentDiceValue));
    }

    IEnumerator AddPower(Player player, int power, Unit unit = null)
    {
        // Deciding whose unit is it
        TextMeshProUGUI powerText = (player == GameManager.instance.player) ? playerPowerText : opponentPowerText;

        // Setting Unit Scale
        Vector3 defaultUnitScale = new Vector3();
        if (unit != null)
        {
            defaultUnitScale = unit.transform.localScale;
            Vector3 highlightedUnitScale = defaultUnitScale * unitScaleMod;
            unit.transform.localScale = highlightedUnitScale;
        }

        // Setting text scale vars
        float startingTextSize = powerText.fontSize;
        float scaledTextSize = startingTextSize * textScaleMod;

        // Increasing power
        player.currentLinePower += power;
        powerText.text = player.currentLinePower.ToString();

        // Making power text bigger and have white color
        float t = 0;

        while (t < timePerUnit)
        {
            t += Time.deltaTime;
            float actualT = t / timePerUnit;
            float coolT = actualT * actualT;

            // Increasing text size
            float currentFontSize = Mathf.RoundToInt(Mathf.Lerp(startingTextSize, scaledTextSize, coolT));
            powerText.fontSize = currentFontSize;

            // Changing text color (from player color to white)
            Color currentTextColor = Color.Lerp(player.playerColor, Color.white, coolT);
            powerText.color = currentTextColor;

            yield return null;
        }

        // Quickly making text back to normal
        t = 0;

        while (t < timePerUnit)
        {
            t += Time.deltaTime * 2;
            float actualT = t / timePerUnit;
            float coolT = actualT * actualT;

            // Increasing text size
            float currentFontSize = Mathf.RoundToInt(Mathf.Lerp(scaledTextSize, startingTextSize, coolT));
            powerText.fontSize = currentFontSize;

            // Changing text color (from player color to white)
            Color currentTextColor = Color.Lerp(Color.white, player.playerColor, coolT);

            yield return null;
        }

        powerText.fontSize = startingTextSize;
        powerText.color = player.playerColor;

        // variables back to normal
        if (unit != null) unit.transform.localScale = defaultUnitScale;

        // pause between units
        yield return new WaitForSeconds(0.5f);
    }
}
