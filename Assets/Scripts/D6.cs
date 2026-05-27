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

    private void Start()
    {
        sprite.color = unit.card.player.playerColor;
    }
    public IEnumerator RollAnimation()
    {
        Vector3 diceStartingPos = transform.position;
        Vector3 diceTargetPos = new Vector3(diceStartingPos.x, diceStartingPos.y + rollDistance, diceStartingPos.z);
        rolling = true;

        // randomly picking dice values while it is rolling
        StartCoroutine(RandomizeDiceValue());

        yield return new WaitForSeconds(rollingTime);
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

        rolling = false;

        // snapping to correct position and rotation
        transform.position = diceStartingPos;
        sprite.transform.localRotation = Quaternion.identity;

        // pop animation at the end
        float popAnimIntensity = (GetDiceValue() == 6) ? 0.007f : 0.004f;
        Animations.instance.PopAnim(this.gameObject, 0.3f, popAnimIntensity);

        // playing sound effect
        string sfxName = "DiceRollSFX";

        if (GetDiceValue() == 1) sfxName = "NegativeSFX";
        //else if (GetDiceValue() == 6) sfxName = "sixRollSFX";
        AudioManager.instance.PlaySFX(sfxName);

        // adding rolled power
        unit.card.GainPower(GetDiceValue(), false);

        // little pause after rolling
        yield return new WaitForSeconds(pauseAfterRolling);

        // triggering event
        GameManager.instance.BroadcastOnDiceRolled(unit.card, GetDiceValue());
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
