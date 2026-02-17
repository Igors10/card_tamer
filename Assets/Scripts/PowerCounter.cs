using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PowerCounter : MonoBehaviour
{
    [Header("refs")]
    public Player player;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] Image powerIcon;
    [HideInInspector] public float currentPower;
    [SerializeField] D6 dice;

    [Header("default vals")]
    Vector3 defaultIconScale;
    float defaultFontSize;

    [Header("Adding power")]
    [SerializeField] float unitScaleMod = 1.2f;
    [SerializeField] float textScaleMod = 1.5f;
    [SerializeField] float timePerUnit = 0.5f;
    [HideInInspector] public bool diceRolled;

    [Header("Resolving")]
    [SerializeField] float resolveSizeMod = 1.5f;
    [SerializeField] Color lostColor;
    [SerializeField] ParticleSystem winVFX;
    [SerializeField] ParticleSystem damageVFX;
    [SerializeField] float damageAnimTime;
    [SerializeField] float powerDecreaseInterval;
    [SerializeField] GameObject damageObj;
    [HideInInspector] public bool resolved;
    private void Start()
    {
        // passing reference to this to player
        player.powerCounter = this;
    }

    private void OnEnable()
    {
        // saving default values
        defaultFontSize = powerText.fontSize;
        defaultIconScale = powerIcon.transform.localScale;
    }

    /// <summary>
    /// Resetting values to default
    /// </summary>
    public void ResetCounter()
    {
        if (defaultFontSize != 0) powerText.fontSize = defaultFontSize;
        powerText.color = player.playerColor;
        powerIcon.color = player.playerColor;
        if (defaultIconScale != Vector3.zero) powerIcon.transform.localScale = defaultIconScale;
        powerText.text = "0";
        currentPower= 0;
        diceRolled = false;
        resolved = false;
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

    public void ResolveCounter(bool won, Field field)
    {
        // When player loses (has less power on the current line
        if (!won)
        {
            powerIcon.transform.localScale /= resolveSizeMod;
            powerText.fontSize = powerText.fontSize / resolveSizeMod;
            powerText.color = lostColor;
            powerIcon.color = lostColor;
            resolved = true;
            return;
        }

        // When player wins (has more power)
        powerIcon.transform.localScale *= resolveSizeMod;
        powerText.fontSize = powerText.fontSize * resolveSizeMod;
        if (winVFX != null) winVFX.Play();

        // Dealing damage to opponent field
        StartCoroutine(DealFieldDamage(field));
    }

    IEnumerator DealFieldDamage(Field field)
    {
        // Damage absorbed by block
        if (field.currentBlock > 0)
        {
            Vector3 blockPosition = Camera.main.WorldToScreenPoint(field.blockObj.transform.position);
            yield return StartCoroutine(Damage(blockPosition, field.currentBlock));
            yield return new WaitForSeconds(0.5f);
        }

        // Damage to units
        List<Unit> unitsToDamage = field.GetFieldUnits();
        for (int i = 0; i < unitsToDamage.Count; i++)
        {
            // checking if there is any power left
            if (currentPower == 0) break;

            int unitHP = unitsToDamage[i].card.GetCurrerntHealth();
            int damage = (currentPower > unitHP) ? unitHP : (int)currentPower;
            Vector3 unitPosition = Camera.main.WorldToScreenPoint(unitsToDamage[i].transform.position);
            yield return StartCoroutine(Damage(unitPosition, damage));
            yield return StartCoroutine(unitsToDamage[i].TakeDamage(damage));
            yield return new WaitForSeconds(0.5f);
        }

        // Damage to player HP

        // Mark as resolved
        resolved = true;
    }

    /// <summary>
    /// Flying out a sword particle to indicate which object is getting damaged
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <returns></returns>
    IEnumerator Damage(Vector3 targetPosition, int damageAmount)
    {
        StartCoroutine(DecreasePower(damageAmount));

        float t = 0;
        Vector3 startingPos = powerText.transform.position;
        damageObj.SetActive(true);
        damageObj.GetComponent<Image>().color = player.playerColor;
        
        while (t < damageAnimTime)
        {
            t += Time.deltaTime;
            float actualT = t / damageAnimTime;
            float coolT = actualT * actualT;

            damageObj.transform.position = Vector3.Lerp(startingPos, targetPosition, coolT);
            yield return null;
        }

        damageObj.SetActive(false);
        // playing the damange VFX
        if (damageVFX != null)
        {
            damageVFX.transform.position = targetPosition;
            damageVFX.Play();
        }
    }

    /// <summary>
    /// Counting down power one by one when power is decreased;
    /// </summary>
    /// <param name="decrease"></param>
    /// <returns></returns>
    IEnumerator DecreasePower(int decrease)
    {
        float targetPower = currentPower - decrease;
        if (targetPower < 0) targetPower = 0;

        while (currentPower != targetPower)
        {
            currentPower--;
            powerText.text = currentPower.ToString();
            yield return new WaitForSeconds(powerDecreaseInterval);
        }
    }
}
