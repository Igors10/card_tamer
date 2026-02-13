using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerCounter : MonoBehaviour
{
    [Header("refs")]
    public Player player;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] Image powerIcon;
    [HideInInspector] public float currentPower;
    [SerializeField] D6 dice;

    [Header("Adding power")]
    [SerializeField] float unitScaleMod = 1.2f;
    [SerializeField] float textScaleMod = 1.5f;
    [SerializeField] float timePerUnit = 0.5f;
    [HideInInspector] public bool diceRolled;

    private void Start()
    {
        // passing reference to this to player
        player.powerCounter = this;
    }

    /// <summary>
    /// Resetting values to default
    /// </summary>
    public void ResetCounter()
    {
        powerText.color = player.playerColor;
        powerIcon.color = player.playerColor;
        powerText.text = "0";
        currentPower= 0;
        diceRolled = false;
        EnableDice(false);
    }

    /// <summary>
    /// Enables dice to be clicked on
    /// </summary>
    public void EnableDice(bool enable)
    {
        dice.gameObject.SetActive(enable);
        dice.clickable = enable;
    }

    /// <summary>
    /// Rolls the dice and adds its value to power
    /// </summary>
    /// <returns></returns>
    public IEnumerator RollDicePower()
    {
        EnableDice(true);
        dice.clickable = false;
        yield return StartCoroutine(dice.RollAnimation());
        yield return StartCoroutine(AddPower(dice.GetDiceValue()));

        diceRolled = true;
    }

    public IEnumerator AddPower(int power, Unit unit = null)
    {
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
        currentPower += power;
        powerText.text = currentPower.ToString();

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
            float coolT = 1 - (1 - actualT) * (1 - actualT);

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
