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
    [SerializeField] Image counterBG;
    [HideInInspector] public float currentPower;
    [SerializeField] D6 dice;
    public GameObject playerCounterVisuals;

    [Header("default vals")]
    Vector3 defaultIconScale;
    float defaultFontSize;
    Color powerTextColor = Color.white;

    [Header("Adding power")]
    [SerializeField] float unitScaleMod = 1.2f;
    [SerializeField] float textScaleMod = 1.5f;
    [SerializeField] float counterScaleMod = 0.1f;
    [SerializeField] float timePerUnit = 0.5f;
    [HideInInspector] public bool diceRolled;

    [Header("Resolving")]
    [SerializeField] float resolveSizeMod = 1.5f;
    [SerializeField] Color lostColor;
    [SerializeField] ParticleSystem winVFX;
    [SerializeField] ParticleSystem damageVFX;
    [SerializeField] float damageAnimTime;
    [SerializeField] float powerDecreaseInterval;
    [HideInInspector] public bool resolved;
    [SerializeField] int damagePowerCost; 
    List<GameObject> damageParticles = new List<GameObject>();
    [HideInInspector] public float damageParticleOffset = 40f;
    private void Start()
    {
        // passing reference to this to player
        player.powerCounter = this;
    }

    private void OnEnable()
    {
        // saving default values
        defaultFontSize = powerText.fontSize;
        defaultIconScale = counterBG.transform.localScale;
        powerTextColor = Colors.instance.BlendColor(player.playerColor, 0.25f);
    }

    /// <summary>
    /// Resetting values to default
    /// </summary>
    public void ResetCounter()
    {
        if (defaultFontSize != 0) powerText.fontSize = defaultFontSize;
        powerText.color = powerTextColor;
        counterBG.color = player.playerColor;
        if (defaultIconScale != Vector3.zero) counterBG.transform.localScale = defaultIconScale;
        powerText.text = "0";
        currentPower= 0;
        transform.localScale = new Vector3(1, 1, 1);
        diceRolled = false;
        resolved = false;
    }

    public void RefreshCounterScale()
    {
        PowerCounter opponentCounter = GameManager.instance.GetOpponentOfPlayer(player).powerCounter;

        // getting power difference
        float opponentPower = opponentCounter.currentPower;
        float powerDifference = currentPower - opponentPower;

        // refreshing scale of the powerCounter
        float newCounterScale = 1 + powerDifference * counterScaleMod;
        if (newCounterScale < 0.1f) newCounterScale = 0.1f;
        transform.localScale = new Vector3(newCounterScale, newCounterScale, 1f);
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
            Color currentTextColor = Color.Lerp(powerTextColor, Color.white, coolT);
            powerText.color = currentTextColor;

            yield return null;
        }

        // Increasing power
        for (int i = 0; i < power; i++)
        {
            currentPower++;
            powerText.text = currentPower.ToString();

            // refreshing counter size to show difference in power
            RefreshCounterScale();
            GameManager.instance.GetOpponentOfPlayer(player).powerCounter.RefreshCounterScale();

            yield return new WaitForSeconds(0.13f);
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
        powerText.color = powerTextColor;

        // variables back to normal
        if (unit != null) unit.transform.localScale = defaultUnitScale;

        // pause between units
        yield return new WaitForSeconds(0.5f);
    }

    public void ResolveCounter(bool won, Field[] field)
    {
        // When player loses (has less power on the current line
        if (!won)
        {
            //Animations.instance.PopAnim(this.gameObject, 0.45f, -0.2f);
            powerText.fontSize = powerText.fontSize / resolveSizeMod;
            powerText.color = Color.white;
            counterBG.color = lostColor;
            resolved = true;

            // Removing power from losing units
            List<Unit> losingUnits = player.fields[GameManager.instance.battleManager.currentLine].GetFieldUnits();
            foreach (Unit unit in losingUnits) { unit.card.currentPower = 0; unit.RefreshUnitVisuals(); }

            return;
        }

        // When player wins (has more power)
        Animations.instance.PopAnim(this.gameObject, 0.45f, 0.4f);
        powerText.fontSize = powerText.fontSize * resolveSizeMod;
        if (winVFX != null) winVFX.Play();

        // Dealing damage to opponent field
        StartCoroutine(DealFieldDamage(field));
    }

    IEnumerator DealFieldDamage(Field[] field)
    {
        //yield return new WaitForSeconds(0.5f);

        // KNOCK OUT ENEMY UNITS
        // =====================
        List <Unit> enemyUnits = field[1].GetFieldUnits();

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            StartCoroutine(enemyUnits[i].KnockOut());
            //yield return new WaitForSeconds(0.2f);
        }
        //yield return new WaitForSeconds(0.3f);

        // DAMAGE TO ENEMY PLAYER 
        // ======================
        int friendlyUnitCount = field[0].GetFieldUnits().Count;

        // preparing damage particles
        for (int i = 0; i < friendlyUnitCount; i++)
        {
            // spawning damage particle
            Vector3 unitPos = Camera.main.WorldToScreenPoint(field[0].GetFieldUnits()[i].transform.position);
            GameObject newDamageParticle = Instantiate(GameManager.instance.battleManager.dmgParticlePrefab, unitPos, Quaternion.identity, GameManager.instance.battleManager.damageObj.transform);
            newDamageParticle.GetComponent<Image>().color = player.playerColor;
            newDamageParticle.transform.localPosition += new Vector3(damageParticleOffset, 0f, 0f);
            damageParticles.Add(newDamageParticle);

            // playing soundeffect
            AudioManager.instance.PlaySFX("MoreDamageSFX");

            // pop animation
            GameManager.instance.animations.PopAnim(newDamageParticle, 0.3f, 0.45f);
            yield return new WaitForSeconds(0.2f);
        }

        // dealing damage to opponent 
        for (int i = 0; i < friendlyUnitCount; i++)
        {
            // Visuals for damage targeting opponents UI
            Player playerToDamage = GameManager.instance.GetOpponentOfPlayer(player);
            Vector3 healthbarPos = playerToDamage.playerUI.healthbar.transform.position;
            yield return Damage(healthbarPos, damageParticles[i]);

            // Refreshing opponents hp value and applying damage juice effects
            playerToDamage.TakeDamage(1);
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
  
        // destroying excess damage particles
        foreach (GameObject damageParticle in damageParticles) { Destroy(damageParticle); }

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
    public IEnumerator Damage(Vector3 targetPosition, GameObject damagePart)
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
    IEnumerator DecreasePower(int decrease, float decreaseSpeed = 1f)
    {
        float targetPower = currentPower - decrease;
        if (targetPower < 0) targetPower = 0;

        while (currentPower != targetPower)
        {
            currentPower--;
            powerText.text = currentPower.ToString();
            yield return new WaitForSeconds(powerDecreaseInterval / decreaseSpeed);
        }
    }
}
