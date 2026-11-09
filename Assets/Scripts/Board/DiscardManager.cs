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

    [Header("rows")]
    [SerializeField] int rowSize = 6; // most cards shown in a row
    [SerializeField] float rowWidth = 1000f;
    [SerializeField] float maxDoodleSpacing = 325f; // with few cards they are this far apart
    [SerializeField] float maxDoodleScale = 1.3f; // and this big
    [SerializeField] float rowFadeTime = 0.3f; // one player's cards fade out, the other's fade in

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

    /// <summary>
    /// Picks which knocked out card gets destroyed (index in cardsInDiscard), -1 if there are none
    /// </summary>
    public int RollAutoDiscard(Player player)
    {
        if (player.cardsInDiscard.Count == 0) return -1;
        return GameRandom.Range(0, player.cardsInDiscard.Count);
    }

    /// <summary>
    /// After the round: each player's knocked out cards are shown one player after the other (host first on both games),
    /// Scissor Rat destroys one of them. Picks are rolled before (RollAutoDiscard), -1 = that player lost no cards
    /// </summary>
    public IEnumerator StartRoundDiscard(int playerPick, int opponentPick)
    {
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;

        // nobody lost a card, nothing to destroy
        if (playerPick < 0 && opponentPick < 0) yield break;

        // little pause before enabling the discards
        yield return new WaitForSeconds(beforeDiscardTime);

        bool playerFirst = GameData.localSeat == 0 || GameManager.instance.playerConfig.offlineMatch;
        if (playerFirst)
        {
            yield return RoundDiscardFor(player, playerPick);
            yield return RoundDiscardFor(opponent, opponentPick);
        }
        else
        {
            yield return RoundDiscardFor(opponent, opponentPick);
            yield return RoundDiscardFor(player, playerPick);
        }

        yield return HideDiscardUI();
        FinishDiscard();
    }

    /// <summary>
    /// One player's knocked out cards: Scissor Rat destroys the picked one (-1 = this player lost no cards).
    /// The discard screen stays open between the players, only the cards fade out and the next ones fade in
    /// </summary>
    IEnumerator RoundDiscardFor(Player owner, int pick)
    {
        if (pick < 0) yield break;

        // the previous player's cards fade away
        bool screenOpen = discardUI.activeSelf;
        if (screenOpen)
        {
            yield return new WaitForSeconds(afterDiscardTime);
            discardPointer.SetActive(false);
            yield return FadeRow(currentUnits, false);
        }

        StartDiscard(true);
        List<UnitAtDiscard> row = AssignRow(owner.cardsInDiscard, owner, 0, 0f, owner.cardsInDiscard.Count);
        discardText.text = "Scissor Rat destroys one of " + ((owner == GameManager.instance.player) ? "your" : "the opponent's") + " knocked out cards";
        if (screenOpen) yield return FadeRow(row, true);

        yield return AutoDiscard(row[pick]);
    }

    /// <summary>
    /// Fades a row of cards in or out
    /// </summary>
    IEnumerator FadeRow(List<UnitAtDiscard> row, bool fadeIn)
    {
        float t = 0;
        while (t < rowFadeTime)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(t / rowFadeTime);
            foreach (UnitAtDiscard unit in row) unit.SetAlpha(fadeIn ? alpha : 1 - alpha);
            yield return null;
        }
        foreach (UnitAtDiscard unit in row) unit.SetAlpha(fadeIn ? 1f : 0f);
    }

    /// <summary>
    /// Workshop: the player picks a card from hand to destroy for a coin
    /// </summary>
    public IEnumerator StartDiscardSequence(Player player)
    {
        StartDiscard(false);
        AssignRow(player.cardsInHand, player, 0, 0f, player.cardsInHand.Count);
        discardText.text = "Choose a card to destroy for 1 coin";

        // not finishing the coroutine before the discard is finished so that the workshop waits for it
        while (discarding) { yield return null; }
    }

    void StartDiscard(bool randomDiscard)
    {
        discarding = true;
        currentDiscardRandom = randomDiscard;
        unitWasCut = false;
        currentUnits.Clear();

        // the close button only when the player picks
        closeButton.gameObject.SetActive(!randomDiscard);

        foreach (UnitAtDiscard unit in units) unit.gameObject.SetActive(false);
        discardUI.SetActive(true);
        discardAvailable = true;
    }

    /// <summary>
    /// Shows the cards in a row, using the discard slots from firstUnit on. Returns the slots in card order
    /// </summary>
    List<UnitAtDiscard> AssignRow(List<Card> cards, Player player, int firstUnit, float rowY, int layoutCount)
    {
        List<UnitAtDiscard> row = new List<UnitAtDiscard>();

        // the more cards, the closer together and smaller they are
        float spacing = (layoutCount > 0) ? Mathf.Min(maxDoodleSpacing, rowWidth / layoutCount) : maxDoodleSpacing;
        float scale = maxDoodleScale * spacing / maxDoodleSpacing;

        for (int i = 0; i < cards.Count && i < rowSize; i++)
        {
            UnitAtDiscard unit = units[firstUnit + i];
            unit.gameObject.SetActive(true);
            ((RectTransform)unit.transform).anchoredPosition = new Vector2(spacing * (i - (cards.Count - 1) / 2f), rowY);
            unit.SetCard(cards[i]);
            unit.SetAlpha(1f);
            unit.SetRestScale(Vector3.one * scale);
            unit.Discard(false);
            unit.lockInteraction = currentDiscardRandom;
            currentUnits.Add(unit);
            row.Add(unit);
        }
        return row;
    }

    /// <summary>
    /// If player decides not to discard a card, this closes the discard UI
    /// </summary>
    public void CloseDiscardWindow()
    {
        StartCoroutine(CloseSequence());
    }

    IEnumerator CloseSequence()
    {
        yield return HideDiscardUI();
        FinishDiscard();
    }

    IEnumerator HideDiscardUI()
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

        discardPointer.SetActive(false);
        discardUI.SetActive(false);
        // hiding the card preview, cards don't get the pointer exit event when the window closes under the cursor
        GameManager.instance.managerUI.PreviewCard(false, null, null, Vector3.zero);
    }

    void FinishDiscard()
    {
        discarding = false;
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
    /// Scissor Rat thinks for a moment, then cuts the picked card
    /// </summary>
    IEnumerator AutoDiscard(UnitAtDiscard chosenUnit)
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

                // every jump sounds a little higher
                MovePointer(Vector3.zero, true, Mathf.Lerp(pointerStartPitch, pointerEndPitch, progress));
            }
            yield return null;
        }


        // the picked card trembles for a moment and gets cut
        discardAvailable = false;
        MovePointer(chosenUnit.transform.position, false, pointerEndPitch);
        yield return chosenUnit.Tremble(finalTrembleTime, finalTrembleIntensity);

        // Lucky: the card stays, its Lucky sticker is used up instead
        Card card = chosenUnit.storedCard;
        Sticker lucky = card.GetStickerWith<ProtectFromDiscardEffect>();
        if (lucky != null)
        {
            card.RemoveSticker(lucky);
            chosenUnit.SetCard(card); // the shown card loses the sticker too
            Animations.instance.PopAnim(chosenUnit.gameObject, 0.4f, 0.3f);
            AudioManager.instance.PlaySFX("ShopStarSFX");
            discardText.text = card.cardData.name + " is lucky and loses its Lucky sticker instead";
            yield break;
        }

        yield return DiscardUnitSequence(chosenUnit);
        discardText.text = card.cardData.name + " is destroyed";
    }

    /// <summary>
    /// Workshop: the player clicked a card to destroy for a coin
    /// </summary>
    public void DiscardUnit(UnitAtDiscard unit)
    {
        // locking the discard so nothing else can be clicked while the cut plays out
        discardAvailable = false;
        closeButton.gameObject.SetActive(false);

        StartCoroutine(ManualDiscardSequence(unit));
    }

    IEnumerator ManualDiscardSequence(UnitAtDiscard unit)
    {
        string cardName = unit.storedCard.cardData.name;
        yield return DiscardUnitSequence(unit);
        discardText.text = cardName + " is destroyed for +1 coin";

        // flying the shop star reward into the counter
        yield return FlyRewardStar(unit.transform.position);
        GameManager.instance.shopManager.AddShopStar();

        StartCoroutine(CloseSequence());
    }

    /// <summary>
    /// Cuts the unit in half and destroys its card
    /// </summary>
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

        // online the other game destroys the same card (workshop discard only, the knocked out discard is rolled on both games)
        if (!currentDiscardRandom && !GameManager.instance.playerConfig.offlineMatch)
            MatchNetwork.instance.SendDiscardFromHand(unit.storedCard.player.cardsInHand.IndexOf(unit.storedCard));

        // removing the card from game
        unit.storedCard.DestroyCard();

        GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);

        // hit stop: freezing everything for a split second right as the scissors cut
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(hitStopTime);
        Time.timeScale = 1f;
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
