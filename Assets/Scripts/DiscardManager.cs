using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscardManager : MonoBehaviour
{
    [Header("animation times")]
    [SerializeField] float ratThinkingTime;
    [SerializeField] float beforeDiscardTime;
    [SerializeField] float afterDiscardTime;
    [SerializeField] float unitsMovingTime;
    [SerializeField] float pointerInterval; // how fast the pointer jumps at the start of the rat's thinking

    [Header("juice")]
    [SerializeField] float pointerSlowestInterval = 0.45f; // pointer slows down to this like a prize wheel
    [SerializeField] float pointerStartPitch = 0.9f;
    [SerializeField] float pointerEndPitch = 1.4f;
    [SerializeField] float finalTrembleTime = 0.6f; // chosen unit shakes before getting cut
    [SerializeField] float finalTrembleIntensity = 10f;
    [SerializeField] float hitStopTime = 0.08f; // everything freezes for a split second when the scissors cut
    [SerializeField] float afterManualCutTime = 0.7f;
    [SerializeField] float rewardStarSize = 80f;
    [SerializeField] float rewardStarHangTime = 0.25f;
    [SerializeField] float rewardStarFlyTime = 0.6f;
    [SerializeField] float rewardStarTrailInterval = 0.03f;
    [SerializeField] float rewardStarTrailFadeTime = 0.25f;

    [Header("refs")]
    [SerializeField] DeathAnim death;
    [SerializeField] UnitAtDiscard[] units;
    public GameObject previewPoint;
    [SerializeField] GameObject discardUI;
    public GameObject discardPointer;
    [SerializeField] Button closeButton;
    [SerializeField] TextMeshProUGUI discardText;

    List<UnitAtDiscard> currentUnits = new List<UnitAtDiscard>();
    bool currentDiscardRandom = true;
    [HideInInspector] public bool discardAvailable = false;
    bool discarding = false;
    bool unitWasCut = false;

    private void Start()
    {
        // starting the unit discard
        GameManager.instance.discardManager = this;
    }

    private void OnDisable()
    {
        // never leaving the game frozen if the hit stop gets interrupted
        Time.timeScale = 1f;
    }

    public IEnumerator StartDiscardSequence(Player player, bool randomDiscard)
    {
        // little pause before enabling the discards
        if (randomDiscard) yield return new WaitForSeconds(beforeDiscardTime);

        discarding = true;
        currentDiscardRandom = randomDiscard;
        unitWasCut = false;

        // Enabling the discard button if the discard is not random
        closeButton.gameObject.SetActive(!randomDiscard);

        // discard player units
        List<Card> listOfCardsForDiscard = (randomDiscard) ? player.cardsInDiscard : player.cardsInHand;
        AssignDiscardedUnits(listOfCardsForDiscard, player, randomDiscard);

        // Changing workshop hint
        string hintMessage = (randomDiscard) ? "Scissor Rat destroys one knocked card" : "Choose a card to destroy for 1 coin";
        discardText.text = hintMessage;

        if (randomDiscard && GameManager.instance.player.cardsInDiscard.Count == 0) StartCoroutine(FinishDiscard());
        else if (randomDiscard) StartCoroutine(AutoDiscard(player));

        // not finishing the coroutine before the discard is finished so that the battle phase doesnt change for the shop
        while (discarding) { yield return null; }
    }

    /// <summary>
    /// Makes discarded units of chosen player appear on the discard UI
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    void AssignDiscardedUnits(List<Card> listOfCards, Player player, bool lockHoveringOver)
    {
        // deactivating units 
        foreach (UnitAtDiscard unit in units) unit.gameObject.SetActive(false);

        // if no units lost then skip discard for this player
        if (listOfCards.Count < 1)
        {
            StartCoroutine(FinishDiscard());
            return;
        }

        // enabling the discard available
        discardUI.SetActive(true);
        discardAvailable = true;

        // INITIALYZING UNITS
        List<Vector3> unitDiscardPos = new List<Vector3>();        

        for (int i = 0; i < listOfCards.Count; i++)
        {
            // activating units
            units[i].gameObject.SetActive(true);
            units[i].sprite.RefreshSprite(listOfCards[i].cardData.unitSprite, player.playerColor, listOfCards[i].cardData.secondaryColor);
            units[i].storedCard = listOfCards[i];
            units[i].Discard(false);
            units[i].lockInteraction = lockHoveringOver;
            currentUnits.Add(units[i]);

            // assigning units to a position
            Vector3 newUnitPosition = units[i].transform.position + new Vector3(Random.Range(-40, 40), Random.Range(-40, 40), 0f);
            unitDiscardPos.Add(newUnitPosition);
        }
    }

    /// <summary>
    /// If player decides not to discard a card, this closes the discard UI
    /// </summary>
    public void CloseDiscardWindow()
    {
        StartCoroutine(FinishDiscard());
    }

    IEnumerator FinishDiscard()
    {
        // pause to let all the anims play out and let player understand which card was discarded
        if (currentDiscardRandom) yield return new WaitForSeconds(afterDiscardTime);
        else if (unitWasCut) yield return new WaitForSeconds(afterManualCutTime);

        // Animating the discard UI disappearance
        float fadeOutTime = afterDiscardTime / 3;
        if (discardUI.activeSelf && (currentDiscardRandom || unitWasCut))
        {
            discardUI.GetComponent<AutoFade>().FadeOut(fadeOutTime);
            yield return new WaitForSeconds(fadeOutTime);
        }

        // resetting discard vals
        currentUnits.Clear();
        discardAvailable = false;
        discarding = false;

        discardPointer.SetActive(false);
        discardUI.SetActive(false);
        GameManager.instance.shopManager.cardDisarded = true;

        // refreshin the shop (to see if the discard button still available)
        GameManager.instance.shopManager.Refresh();
        //GameManager.instance.managerUI.workshop.ChangeHint("Create new cards!");
    }

    public UnitAtDiscard MovePointer(Vector3 unitPos, bool randomUnitPos = false, float pitch = 0f)
    {
        if (currentUnits.Count < 1) return null;

        // enabling the pointer
        discardPointer.SetActive(true);
        
        // moving the pointer to a random different unit
        Vector3 newPointerPosition = unitPos;

        if (randomUnitPos) // choosing a random position 
        {
            do
            {
                newPointerPosition = currentUnits[Random.Range(0, currentUnits.Count)].transform.position;
            } while (newPointerPosition == discardPointer.transform.position && currentUnits.Count != 1); // making sure the new chosen unit is different from the prev one
        }
        discardPointer.transform.position = newPointerPosition;

        // Playing SFX
        if (currentUnits.Count > 1) AudioManager.instance.PlaySFX("RerollSFX", 0, pitch);

        return currentUnits.Find(x => x.transform.position == newPointerPosition);
    }

    /// <summary>
        /// Lets the game choose discarded units for the player;
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
    public IEnumerator AutoDiscard(Player player)
    {
        yield return Animations.instance.SkippablePause(beforeDiscardTime);
        Debug.Log("DiscardManager: auto discard started");


        // Pointer randomly moving around simulating rat choosing a target, slowing down like a prize wheel
        float t = 0;
        float pointerT = 0;
        float currentInterval = pointerInterval;

        while (t < ratThinkingTime)
        {
            t += Time.deltaTime;
            pointerT += Time.deltaTime;

            // jumping the pointer to another unit when interval time is reached
            if (pointerT >= currentInterval)
            {
                pointerT = 0;
                float progress = Mathf.Clamp01(t / ratThinkingTime);
                currentInterval = Mathf.Lerp(pointerInterval, pointerSlowestInterval, progress * progress);

                // every jump sounds a little higher and the unit flinches
                UnitAtDiscard pointedUnit = MovePointer(Vector3.zero, true, Mathf.Lerp(pointerStartPitch, pointerEndPitch, progress));
                if (pointedUnit != null) pointedUnit.Flinch();
            }
            yield return null;
        }


        // choosing random unit, letting it tremble for a moment and discarding it
        int randomUnitID = Random.Range(0, currentUnits.Count);
        UnitAtDiscard chosenUnit = currentUnits[randomUnitID].GetComponentInParent<UnitAtDiscard>();
        MovePointer(chosenUnit.transform.position, false, pointerEndPitch);
        yield return chosenUnit.Tremble(finalTrembleTime, finalTrembleIntensity);

        DiscardUnit(chosenUnit);
    }

    public void DiscardUnit(UnitAtDiscard unit)
    {
        // locking the discard so nothing else can be clicked while the cut plays out
        discardAvailable = false;
        closeButton.gameObject.SetActive(false);

        StartCoroutine(DiscardUnitSequence(unit));
    }

    IEnumerator DiscardUnitSequence(UnitAtDiscard unit)
    {
        Debug.Log("DiscardManager: unit discarded");
        unitWasCut = true;

        // cutting the unit in half (before the discard visuals, so the halves keep the unit's colors)
        unit.CutInHalf();

        // dead unit animation
        unit.Discard(true);

        // making death point finger at the unit
        StartCoroutine(death.PointAnim(afterDiscardTime / 2));
        // displaying destroyed units name
        string newHintMessage = unit.storedCard.cardData.name + " is destroyed";
        if (!currentDiscardRandom) newHintMessage += " for +1 coin";
        discardText.text = newHintMessage;

        // removing the card from game
        unit.storedCard.DestroyCard();

        GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);

        // hit stop: freezing everything for a split second right as the scissors cut
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;

        // flying the shop star reward into the counter when destroying the card was player's choice
        if (!currentDiscardRandom)
        {
            yield return FlyRewardStar(unit.transform.position);
            GameManager.instance.shopManager.AddShopStar();
        }

        StartCoroutine(FinishDiscard());
    }

    /// <summary>
    /// Shop star pops out of the destroyed unit and flies into the shop star counter
    /// </summary>
    IEnumerator FlyRewardStar(Vector3 startingPos)
    {
        ShopManager shop = GameManager.instance.shopManager;
        GameObject starObj = new GameObject("rewardStar", typeof(RectTransform), typeof(Image));
        RectTransform star = starObj.GetComponent<RectTransform>();
        star.SetParent(discardUI.transform, false);
        star.SetAsLastSibling();
        star.sizeDelta = new Vector2(rewardStarSize, rewardStarSize);
        star.position = startingPos;
        Image starImage = starObj.GetComponent<Image>();
        starImage.sprite = shop.ShopStarSprite;
        starImage.color = shop.ShopStarColor;
        starImage.raycastTarget = false;

        // popping out of the unit
        float t = 0;
        while (t < rewardStarHangTime)
        {
            t += Time.deltaTime;
            float popT = Mathf.Clamp01(t / rewardStarHangTime);
            float overshoot = 1 + 0.4f * Mathf.Sin(popT * Mathf.PI);
            star.localScale = Vector3.one * popT * overshoot;
            yield return null;
        }
        star.localScale = Vector3.one;

        // flying along a curve into the counter, spinning and leaving a trail
        Vector3 targetPos = shop.ShopStarsPosition;
        Vector3 controlPoint = Vector3.Lerp(startingPos, targetPos, 0.5f) + (Vector3)(Random.insideUnitCircle * 300f * star.lossyScale.x);
        float trailTimer = 0;
        t = 0;
        while (t < rewardStarFlyTime)
        {
            t += Time.deltaTime;
            float flyT = Mathf.Clamp01(t / rewardStarFlyTime);
            float coolT = flyT * flyT;

            Vector3 m1 = Vector3.Lerp(startingPos, controlPoint, coolT);
            Vector3 m2 = Vector3.Lerp(controlPoint, targetPos, coolT);
            star.position = Vector3.Lerp(m1, m2, coolT);
            star.Rotate(0f, 0f, -720f * Time.deltaTime);

            trailTimer += Time.deltaTime;
            if (trailTimer >= rewardStarTrailInterval)
            {
                trailTimer = 0;
                StartCoroutine(FadeStarTrail(starImage));
            }
            yield return null;
        }

        Destroy(starObj);
    }

    IEnumerator FadeStarTrail(Image source)
    {
        GameObject trailObj = new GameObject("rewardStarTrail", typeof(RectTransform), typeof(Image));
        RectTransform trail = trailObj.GetComponent<RectTransform>();
        trail.SetParent(source.transform.parent, false);
        trail.SetSiblingIndex(source.transform.GetSiblingIndex()); // rendering behind the star
        trail.sizeDelta = source.rectTransform.sizeDelta;
        trail.position = source.transform.position;
        trail.rotation = source.transform.rotation;
        Image trailImage = trailObj.GetComponent<Image>();
        trailImage.sprite = source.sprite;
        trailImage.raycastTarget = false;
        Color color = source.color;

        float t = 0;
        while (t < rewardStarTrailFadeTime)
        {
            t += Time.deltaTime;
            float fadeT = Mathf.Clamp01(t / rewardStarTrailFadeTime);
            trail.localScale = Vector3.one * 0.8f * (1 - fadeT);
            trailImage.color = new Color(color.r, color.g, color.b, 0.6f * (1 - fadeT));
            yield return null;
        }

        Destroy(trailObj);
    }
}
