using GameKit.Dependencies.Utilities;
using System.Collections;
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
    public IEnumerator RollAnimation()
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

        // automatically stop the die after some time if its AI
        if (unit.card.player.isAI)
        {
            yield return new WaitForSeconds(rollingTime);
            rolling = false;
        }
        // way for input if it is player
        else
        {
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
        diceValue = ChangeDiceValue();

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

        // adding rolled power
        unit.card.startingPower += GetDiceValue();
        unit.card.GainPower(GetDiceValue(), false);

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

    // Die stops rolling when player clicks anywhere
    void DiceInput()
    {
        if (Input.GetButtonDown("Fire1") && unit.card.player == GameManager.instance.player) { rolling = false; Debug.Log("ShopManager: star appearance anim skipped"); }
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
            diceValue = ChangeDiceValue();
            facePunch = faceChangePunch;
            yield return new WaitForSeconds(diceIntervals);
        }
    }

    int ChangeDiceValue()
    {
        int newDiceValue = Random.Range(1, 7);
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
