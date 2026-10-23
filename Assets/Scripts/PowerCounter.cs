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
    [SerializeField] float tickPopIntensity = 0.35f; // how much the number pops on each +1
    [SerializeField] float tickPopTime = 0.12f;
    [SerializeField] float tickBasePitch = 0.9f; // pitch of the first +1 sound, every next +1 is a little higher
    [SerializeField] float tickPitchStep = 0.06f;
    [SerializeField] float tickMaxPitch = 2f;
    Vector3 defaultTextScale;
    Coroutine tickPopRoutine;

    [Header("Resolving")]
    [SerializeField] float resolveSizeMod = 1.5f;
    [SerializeField] Color lostColor;
    [SerializeField] ParticleSystem winVFX;
    [SerializeField] ParticleSystem damageVFX;
    [SerializeField] float damageAnimTime;
    [SerializeField] float powerDecreaseInterval;
    [HideInInspector] public bool resolved;
    [SerializeField] int damagePowerCost; 
    [SerializeField] float loseAnimTime = 0.45f;
    [SerializeField] float loseScaleMod = 0.8f; // how much the counter deflates when losing
    [SerializeField] float loseShakeIntensity = 6f;
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
        defaultTextScale = powerText.transform.localScale;
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
        if (defaultTextScale != Vector3.zero) powerText.transform.localScale = defaultTextScale;
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

        while (t < timePerUnit && !Animations.instance.skipPause)
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

            // pop and a sound that gets higher with every point (not spamming sounds when the animation is skipped)
            PopPowerText();
            if (i == 0 || !Animations.instance.skipPause)
            {
                float pitch = Mathf.Min(tickBasePitch + tickPitchStep * (currentPower - 1), tickMaxPitch);
                AudioManager.instance.PlaySFX("GainPowerSFX", 0, pitch);
            }

            // refreshing counter size to show difference in power
            RefreshCounterScale();
            GameManager.instance.GetOpponentOfPlayer(player).powerCounter.RefreshCounterScale();

            yield return Animations.instance.SkippablePause(0.13f);
        }
        

        // Quickly making text back to normal
        t = 0;

        while (t < timePerUnit && !Animations.instance.skipPause)
        {
            t += Time.deltaTime * 2;
            float actualT = t / timePerUnit;
            float coolT = 1 - (1 - actualT) * (1 - actualT);

            // Increasing text size
            float currentFontSize = Mathf.RoundToInt(Mathf.Lerp(scaledTextSize, startingTextSize, coolT));
            powerText.fontSize = currentFontSize;

            // Changing text color (from white back to the default text color)
            powerText.color = Color.Lerp(Color.white, powerTextColor, coolT);

            yield return null;
        }

        powerText.fontSize = startingTextSize;
        powerText.color = powerTextColor;

        // variables back to normal
        if (unit != null) unit.transform.localScale = defaultUnitScale;

        // pause between units
        yield return Animations.instance.SkippablePause(0.5f);
    }

    /// <summary>
    /// Quick pop of the power number, restarts if it is already popping so the size never drifts
    /// </summary>
    void PopPowerText()
    {
        if (tickPopRoutine != null) StopCoroutine(tickPopRoutine);
        tickPopRoutine = StartCoroutine(PopPowerTextAnim());
    }

    IEnumerator PopPowerTextAnim()
    {
        Transform text = powerText.transform;
        Vector3 poppedScale = defaultTextScale * (1 + tickPopIntensity);
        text.localScale = poppedScale;

        float t = 0;
        while (t < tickPopTime)
        {
            t += Time.deltaTime;
            float clampedT = t / tickPopTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            text.localScale = Vector3.Lerp(poppedScale, defaultTextScale, coolT);
            yield return null;
        }

        text.localScale = defaultTextScale;
        tickPopRoutine = null;
    }

    /// <summary>
    /// Losing counter deflates, wobbles, shakes and fades to the lost color
    /// </summary>
    IEnumerator LoseAnim()
    {
        Animations.instance.ShakeAnim(gameObject, loseShakeIntensity, loseAnimTime);

        Vector3 startingScale = transform.localScale;
        Vector3 targetScale = startingScale * loseScaleMod;
        float startingFontSize = powerText.fontSize;
        float targetFontSize = startingFontSize / resolveSizeMod;
        Color startingTextColor = powerText.color;
        Color startingBGColor = counterBG.color;

        float t = 0;
        while (t < loseAnimTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / loseAnimTime);
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            // deflating with a wobble that dies down
            Vector3 currentScale = Vector3.Lerp(startingScale, targetScale, coolT);
            float wobble = 0.15f * Mathf.Sin(clampedT * Mathf.PI * 3f) * (1 - clampedT);
            Animations.instance.SetSquash(transform, currentScale, wobble);

            powerText.fontSize = Mathf.Lerp(startingFontSize, targetFontSize, coolT);
            powerText.color = Color.Lerp(startingTextColor, Color.white, coolT);
            counterBG.color = Color.Lerp(startingBGColor, lostColor, coolT);
            yield return null;
        }

        transform.localScale = targetScale;
        powerText.fontSize = targetFontSize;
        powerText.color = Color.white;
        counterBG.color = lostColor;
    }

    public void ResolveCounter(bool won, Field[] field)
    {
        // When player loses (has less power on the current line
        if (!won)
        {
            StartCoroutine(LoseAnim());
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
            yield return new WaitForSeconds(0.2f);
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
