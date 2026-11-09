using GameKit.Dependencies.Utilities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class D6 : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Unit unit;
    [SerializeField] Image sprite;
    [SerializeField] Sprite[] diceFaces;
    [SerializeField] GameObject glow;

    [Header("rolling")]
    bool rolling = false;
    int diceValue = 1;
    [SerializeField] float rollingTime;
    [SerializeField] float pauseAfterRolling;
    [SerializeField] float diceIntervals;
    [SerializeField] float rollDistance;
    [SerializeField] float rotationSpeed;
    [HideInInspector] public bool clickable;
    static readonly List<D6> rollingDice = new List<D6>(); // dice waiting for a click (or the online opponent's StopDice), oldest first

    [Header("juice")]
    [SerializeField] float hopHeight = 22f; // how high the die bounces while rolling
    [SerializeField] float hopsPerSecond = 3.5f;
    [SerializeField] float groundSquash = 0.18f; // squash every time the die touches the ground while rolling
    [SerializeField] float faceChangePunch = 0.15f; // little scale punch every time the face changes
    [SerializeField] float landSquash = 0.3f;
    [SerializeField] float landSquashTime = 0.35f;
    [SerializeField] float sixLandMultiplier = 1.6f; // rolling a 6 lands harder
    [SerializeField] float oneShakeIntensity = 8f;
    [SerializeField] float oneShakeTime = 0.35f;
    Vector3 restScale;
    Vector2 spriteRestPos;
    float rollTime;
    float facePunch;

    private void Start()
    {
        sprite.color = unit.card.player.playerColor;
    }

    private void OnDisable()
    {
        // disabled or destroyed mid roll (also the Board being reloaded)
        rollingDice.Remove(this);
        rolling = false;
    }
    /// <summary>
    /// waitForInput: the die waits for a click (bot: stops by itself, online opponent: StopDice command).
    /// Without it the die keeps rolling until Stop is called (dice after the first one stop one by one)
    /// </summary>
    public IEnumerator RollAnimation(bool waitForInput = true)
    {
        Vector3 diceStartingPos = transform.position;
        Vector3 diceTargetPos = new Vector3(diceStartingPos.x, diceStartingPos.y + rollDistance, diceStartingPos.z);
        restScale = transform.localScale;
        spriteRestPos = sprite.rectTransform.anchoredPosition;
        rollTime = 0f;
        facePunch = 0f;
        rolling = true;

        // randomly picking dice values while it is rolling
        StartCoroutine(RandomizeDiceValue());

        // stopped from outside (Stop)
        if (!waitForInput)
        {
            while (rolling) yield return null;
        }
        // automatically stop the die after some time if its AI (only the final roll counts, so timing doesn't matter)
        else if (unit.card.player.isAI)
        {
            yield return new WaitForSeconds(rollingTime);
            rolling = false;
        }
        // wait for input if it is player (online opponent: until their StopDice command comes)
        else
        {
            rollingDice.Add(this);
            while (rolling)
            {
                yield return null;
            }
        }

        /*
        // Floating up
        float t = 0;
        while (t < rollingTime)
        {
            t += Time.deltaTime;
            float actualT = t / rollingTime;
            float coolT = actualT * actualT;

            transform.position = Vector3.Lerp(diceStartingPos, diceTargetPos, coolT);
            yield return null;
        }
        // Floating down
        t = 0;
        while (t < rollingTime)
        {
            t += Time.deltaTime;
            float actualT = t / rollingTime;
            float coolT = 1 - (1 - actualT) * (1 - actualT);

            transform.position = Vector3.Lerp(diceTargetPos, diceStartingPos, coolT);
            yield return null;
        }*/

        // changing dice value again to prevent player from clicking at the correct value
        // this is the only roll that counts, so it comes from GameRandom (same result for both players online)
        diceValue = ChangeDiceValue(GameRandom.Range(1, 7));

        // snapping to correct position, rotation and scale
        transform.position = diceStartingPos;
        sprite.transform.localRotation = Quaternion.identity;
        sprite.rectTransform.anchoredPosition = spriteRestPos;
        sprite.rectTransform.localScale = Vector3.one;
        transform.localScale = restScale;

        // landing thud, harder on a 6, with a shake on a 1
        float landAmount = (GetDiceValue() == 6) ? landSquash * sixLandMultiplier : landSquash;
        StartCoroutine(Animations.instance.SquashStretch(transform, restScale, landAmount, landSquashTime));

        if (GetDiceValue() == 1) Animations.instance.ShakeAnim(sprite.gameObject, oneShakeIntensity, oneShakeTime);

        // playing sound effect
        string sfxName = "DiceRollSFX";

        if (GetDiceValue() == 1) sfxName = "NegativeSFX";
        //else if (GetDiceValue() == 6) sfxName = "sixRollSFX";
        AudioManager.instance.PlaySFX(sfxName);

        // adding rolled power (a power-up gain: number above the counter, green outline, particles)
        unit.card.GainPower(GetDiceValue());

        // little pause after rolling
        yield return new WaitForSeconds(pauseAfterRolling);

        // triggering event
        GameManager.instance.BroadcastOnDiceRolled(unit.card, GetDiceValue());
    }

    private void Update()
    {
        DiceInput();
        AnimateRolling();
    }

    /// <summary>
    /// Makes the die bounce around while rolling, squashing a little every time it hits the ground
    /// </summary>
    void AnimateRolling()
    {
        if (!rolling) return;

        rollTime += Time.deltaTime;
        float hop = Mathf.Abs(Mathf.Sin(rollTime * hopsPerSecond * Mathf.PI));
        sprite.rectTransform.anchoredPosition = spriteRestPos + new Vector2(0f, hop * hopHeight);

        // squash is strongest right when the die touches the ground
        float contact = Mathf.Pow(1 - hop, 6);
        Animations.instance.SetSquash(transform, restScale, contact * groundSquash);

        // face change punch dying down
        facePunch *= Mathf.Exp(-12f * Time.deltaTime);
        sprite.rectTransform.localScale = Vector3.one * (1 + facePunch);
    }

    // Dice stop rolling when player clicks anywhere
    void DiceInput()
    {
        Player owner = unit.card.player;
        if (!Input.GetButtonDown("Fire1") || owner != GameManager.instance.player) return;

        // the oldest rolling die handles the click for all of the player's dice
        int count = CountRolling(owner);
        if (count == 0 || rollingDice.Find(die => die.unit.card.player == owner) != this) return;

        StopDice(owner, count);
        if (!GameManager.instance.playerConfig.offlineMatch) MatchNetwork.instance.SendStopDice(count);
    }

    /// <summary>
    /// Player's dice that are rolling and waiting to be stopped
    /// </summary>
    public static int CountRolling(Player owner)
    {
        return rollingDice.FindAll(die => die.unit.card.player == owner).Count;
    }

    /// <summary>
    /// Stops the player's oldest rolling dice (online the opponent's dice stop when their command comes)
    /// </summary>
    public static void StopDice(Player owner, int count)
    {
        foreach (D6 die in rollingDice.FindAll(die => die.unit.card.player == owner))
        {
            if (count-- <= 0) break;
            die.rolling = false;
            rollingDice.Remove(die);
        }
    }

    /// <summary>
    /// Stops a die that was rolled without waiting for input
    /// </summary>
    public void Stop()
    {
        rolling = false;
    }

    void RotateDice()
    {
        if (!rolling) return;
        RectTransform rect = sprite.GetComponent<RectTransform>();

        float rotationThisFrame =rotationSpeed * Time.deltaTime;
        rect.Rotate(0f, 0f, -rotationThisFrame); 
    }

    void FixedUpdate()
    {
        RotateDice();
    }

    /// <summary>
    /// Changes dice value every now and then
    /// </summary>
    /// <returns></returns>
    IEnumerator RandomizeDiceValue()
    {
        while (rolling)
        {
            // just for show, how many faces flash by depends on timing
            diceValue = ChangeDiceValue(Random.Range(1, 7));
            facePunch = faceChangePunch;
            yield return new WaitForSeconds(diceIntervals);
        }
    }

    int ChangeDiceValue(int newDiceValue)
    {
        sprite.sprite = diceFaces[newDiceValue - 1];

        return newDiceValue;
    }

    public int GetDiceValue()
    {
        return diceValue;
    }

    /// <summary>
    /// Deactivating die's object
    /// </summary>
    public void DisableDie()
    {
        Glow(false);

        // the landing squash might still be playing, so the die has to come back in the resting state
        if (restScale != Vector3.zero) transform.localScale = restScale;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Enables and disables glow around die
    /// </summary>
    /// <param name="glowOn"></param>
    public void Glow(bool glowOn)
    {
        glow.SetActive(glowOn);
    }

    /*
     * // ================== MOUSE INPUT =====================
     * 
    void ManualRoll()
    {
        if (!clickable) return;

        glow.SetActive(false);
        StartCoroutine(powerCounter.RollDicePower());
    }

    void OnHover(bool mouseOver)
    {
        if (!clickable) return;

        glow.SetActive(mouseOver);
    }

    
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ManualRoll();
    }*/
}
