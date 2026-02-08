using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using NUnit.Framework;

public class BattleManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI playerPowerText;
    [SerializeField] TextMeshProUGUI opponentPowerText;

    [Header("Adding power")]
    [SerializeField] float unitScaleMod;
    [SerializeField] int textScaleMod;
    [SerializeField] float timePerUnit;

    int currentLine;

    public void ResetBattleVals()
    {
        currentLine = -1;
        playerPowerText.color = GameManager.instance.player.playerColor;
        opponentPowerText.color = GameManager.instance.opponent.playerColor;
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
            if (units.Count > 0) InitBattleLine(units);

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
            if (oppField.units[i] != null) unitsToReturn.Add(field.units[i]);
        }

        return unitsToReturn;
    }

    /// <summary>
    /// Focuses camera and visuals on current battle line
    /// </summary>
    public IEnumerator InitBattleLine(List<Unit> units)
    {
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
            if (unit.card.currentPower > 0) yield return StartCoroutine(AddUnitPower(unit));
        }

        // ENABLING REROLL
    }   

    IEnumerator AddUnitPower(Unit unit)
    {
        // Deciding whose unit is it
        Player unitOwner = (unit.card.player == GameManager.instance.player) ? GameManager.instance.player : GameManager.instance.opponent;
        TextMeshProUGUI powerText = (unit.card.player == GameManager.instance.player) ? playerPowerText : opponentPowerText;

        // Setting Unit Scale
        Vector3 defaultUnitScale = unit.transform.localScale;
        Vector3 highlightedUnitScale = defaultUnitScale * unitScaleMod;
        unit.transform.localScale = highlightedUnitScale;

        // Setting text scale vars
        float startingTextSize = powerText.fontSize;
        float scaledTextSize = startingTextSize * textScaleMod;

        // Increasing power
        unitOwner.currentLinePower += unit.card.currentPower;
        powerText.text = unitOwner.currentLinePower.ToString();

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
            Color currentTextColor = Color.Lerp(unitOwner.playerColor, Color.white, coolT);

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
            Color currentTextColor = Color.Lerp(Color.white, unitOwner.playerColor, coolT);

            yield return null;
        }

        // variables back to normal
        unit.transform.localScale = defaultUnitScale;
        powerText.fontSize = startingTextSize;
        powerText.color = unitOwner.playerColor;

        // pause between units
        yield return new WaitForSeconds(0.5f);
    }
}
