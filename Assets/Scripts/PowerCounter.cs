using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using FishNet.Demo.AdditiveScenes;

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
    [SerializeField] int damagePowerCost;
    List<GameObject> damageParticles = new List<GameObject>();
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
        // playing soundeffect
        AudioManager.instance.PlaySFX("DiceRollSFX");

        EnableDice(true);
        dice.clickable = false;
        yield return StartCoroutine(dice.RollAnimation());
        yield return StartCoroutine(AddPower(dice.GetDiceValue()));

        diceRolled = true;
    }

    public IEnumerator AddPower(int power, Unit unit = null)
    {
        // playing soundeffect
        AudioManager.instance.PlaySFX("GainPowerSFX");

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

            // Removing power from losing units
            List<Unit> losingUnits = player.fields[GameManager.instance.battleManager.currentLine].GetFieldUnits();
            foreach (Unit unit in losingUnits) { unit.card.currentPower = 0; unit.RefreshUnitVisuals(); }

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
        yield return new WaitForSeconds(0.5f);

        // Damage to opponent
        // Dealing 1 damage for each X power
        int damageToPlayer = 0;
        float damageParticleOffset = 40f;

        while (currentPower > 0)
        {
            int nextDamagePowerCost = (currentPower > damagePowerCost) ? damagePowerCost : (int)currentPower;
            damageToPlayer++;
            int unitToDamageID = 0;

            // playing soundeffect
            AudioManager.instance.PlaySFX("MoreDamageSFX");

            // Adding one damage (sword) particle for each X power is left to show how much damage will be dealt to opposing player
            if (damageToPlayer == 1)
            {
                damageObj.SetActive(true);
                damageParticles.Add(damageObj);
                damageObj.GetComponent<Image>().color = player.playerColor;
                damageObj.transform.position = powerText.transform.position;

                float particleOffsetY = 90f;
                if (player == GameManager.instance.opponent) particleOffsetY *= -1;
                damageObj.transform.localPosition += new Vector3(0f, particleOffsetY, 0f);

                // pop animation
                GameManager.instance.animations.PopAnim(damageObj, 0.3f, 0.45f);
            }
            else
            {
                GameObject newDamageParticle = Instantiate(damageObj, damageObj.transform.position, Quaternion.identity, damageObj.transform);
                newDamageParticle.GetComponent<Image>().color = player.playerColor;
                newDamageParticle.transform.localPosition += new Vector3(damageParticleOffset, 0f, 0f);
                damageParticles.Add(newDamageParticle);

                // pop animation
                GameManager.instance.animations.PopAnim(newDamageParticle, 0.3f, 0.45f);
            }

            
            yield return StartCoroutine(DecreasePower(nextDamagePowerCost));
        }

        // Damage distribution
        for (int i = damageToPlayer; i > 0; i--)
        {
            int currentDmgParticle = i - 1;

            // Damage to block
            if (field.currentBlock > 0)
            {
                // Finding block screen position
                Vector3 blockPosition = Camera.main.WorldToScreenPoint(field.blockObj.transform.position);

                // Calculating damage
                field.currentBlock--;

                // "Attacking" the block
                yield return StartCoroutine(Damage(blockPosition, damageParticles[currentDmgParticle]));

                // playing soundeffect
                AudioManager.instance.PlaySFX("HitSFX");

                // Updating block visuals
                field.RefreshFieldVisuals();

                yield return new WaitForSeconds(0.2f);
                continue;
            }

            // Damage to enemy units
            if (field.GetFieldUnits(true).Count > 1)
            {
                Unit unitToDamage = field.GetFieldUnits()[0];
                
                Vector3 unitPosition = Camera.main.WorldToScreenPoint(unitToDamage.transform.position);

                // "Attacking" the unit
                yield return StartCoroutine(Damage(unitPosition, damageParticles[currentDmgParticle]));
                StartCoroutine(unitToDamage.TakeDamage(1));

                yield return new WaitForSeconds(0.2f);
                continue;
            }
 
            // Damage to enemy player
            // Visuals for damage targeting opponents UI
            Player playerToDamage = GameManager.instance.GetOpponentOfPlayer(player);
            Vector3 healthbarPos = playerToDamage.playerUI.healthbar.transform.position;
            yield return Damage(healthbarPos, damageParticles[currentDmgParticle]);

            // Refreshing opponents hp value and applying damage juice effects
            playerToDamage.TakeDamage(damageToPlayer);
            yield return new WaitForSeconds(0.2f);
        }

       

        yield return new WaitForSeconds(1f);

       


        // destroying excess damage particles
        foreach (GameObject damageParticle in damageParticles) { if (damageParticle != damageParticles[0]) Destroy(damageParticle); }

        // clearing particle list
        damageParticles.Clear();

        // Mark as resolved
        resolved = true;
    }

    /// <summary>
    /// Flying out a sword particle to indicate which object is getting damaged
    /// </summary>
    /// <param name="targetPosition"></param>
    /// <returns></returns>
    IEnumerator Damage(Vector3 targetPosition, GameObject damagePart)
    {
        float t = 0;
        Vector3 startingPos = (damagePart.activeSelf) ? damagePart.transform.position : powerText.transform.position;
        damagePart.SetActive(true);
        damagePart.GetComponent<Image>().color = player.playerColor;

        // Generate random curve variables
        Vector3 midPoint = Vector3.Lerp(startingPos, targetPosition, 0.5f);
        float curveIntensity = Random.Range(300f, 400f);
        Vector3 randomOffset = Random.insideUnitSphere * curveIntensity;
        Vector3 controlPoint = midPoint + randomOffset;

        while (t < damageAnimTime)
        {
            t += Time.deltaTime;
            float actualT = t / damageAnimTime;
            float coolT = actualT * actualT;

            // Bezier Curve movement
            Vector3 m1 = Vector3.Lerp(startingPos, controlPoint, coolT);
            Vector3 m2 = Vector3.Lerp(controlPoint, targetPosition, coolT);
            damagePart.transform.position = Vector3.Lerp(m1, m2, coolT);

            //damageObj.transform.position = Vector3.Lerp(startingPos, targetPosition, coolT);
            yield return null;
        }

        damagePart.SetActive(false);
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
