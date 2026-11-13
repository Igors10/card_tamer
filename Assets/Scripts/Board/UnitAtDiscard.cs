using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class UnitAtDiscard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    public Card card; // read only copy of the card that is shown (and cut)
    [SerializeField] CartoonShakeEffect cartoonShakeEffect;
    [SerializeField] HoverEffect hoverEffect;
    [SerializeField] CardCutter cardCutter;

    [Header("discard attributes")]
    [SerializeField] float discardedUnitSizeMod = 0.75f;

    [HideInInspector] public Card storedCard;
    bool discarded = false;
    [HideInInspector] public bool lockInteraction = false;
    Vector3 defaultScale;
    Vector3 cardRestScale;
    Vector3 cardRestPos;

    private void Awake()
    {
        defaultScale = transform.localScale;
        cardRestScale = card.transform.localScale;
        cardRestPos = card.transform.localPosition;
    }

    /// <summary>
    /// Fading the whole slot (used when the discard switches to the other player's cards)
    /// </summary>
    public void SetAlpha(float alpha)
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = alpha;
    }

    /// <summary>
    /// Shows this card (its owner's colors)
    /// </summary>
    public void SetCard(Card newCard)
    {
        storedCard = newCard;
        card.AssignCardData(newCard.cardData, newCard.player);
    }

    private void OnDisable()
    {
        // halves live next to the unit, so they have to be cleaned up when the unit gets hidden
        RemoveCutHalves();
    }

    /// <summary>
    /// Size the unit has when it's not discarded (rows with many cards use smaller units)
    /// </summary>
    public void SetRestScale(Vector3 scale)
    {
        defaultScale = scale;
        transform.localScale = scale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryDiscard();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool isHover)
    {
        if (discarded || lockInteraction) return; // only during player's turn
        // no new previews while a cut is playing out, the window closes under the cursor and the preview would get stuck
        if (isHover && !GameManager.instance.discardManager.discardAvailable) return;

        // moving discard pointer over it 
        GameManager.instance.discardManager.discardPointer.SetActive(isHover);
        GameManager.instance.discardManager.MovePointer(transform.position);

        // highlighting the card as selectable
        card.HighlightCard(isHover && GameManager.instance.discardManager.discardAvailable);

        // creating card preview above the unit
        Vector3 previewPos = GameManager.instance.discardManager.previewPoint.transform.position;
        GameManager.instance.managerUI.PreviewCard(isHover, storedCard.cardData, storedCard.player, previewPos, false);
    }

    void TryDiscard()
    {
        // can only discard on player's turn and when units have appeared on the screen
        if (!GameManager.instance.yourTurn || !GameManager.instance.discardManager.discardAvailable || discarded || lockInteraction) return;

        // tell discard manager that you want to discard this unit (it flies the shop star reward to the counter after the cut)
        GameManager.instance.discardManager.DiscardUnit(this);
    }

    /// <summary>
    /// Indicates that unit is discarded (or reverses it if false)
    /// </summary>
    /// <param name="isDiscarded"></param>
    public void Discard(bool isDiscarded)
    {
        discarded = isDiscarded;

        if (!isDiscarded) RemoveCutHalves();

        // Disabling the preview and the highlight
        GameManager.instance.managerUI.PreviewCard(false, storedCard.cardData, storedCard.player, Vector3.zero);
        card.HighlightCard(false);

        // chaning the size (always from the default size, so units don't shrink a little more every discard)
        transform.localScale = (isDiscarded) ? defaultScale * discardedUnitSizeMod : defaultScale;

        // disabling cartoon shake animation and hovering functionality
        cartoonShakeEffect.enabled = !isDiscarded;
        hoverEffect.enabled = !isDiscarded;
    }

    // ===============
    // JUICE ANIMATIONS
    // ===============

    /// <summary>
    /// Card shakes harder and harder right before getting cut
    /// </summary>
    public IEnumerator Tremble(float time, float maxIntensity)
    {
        Transform cardTransform = card.transform;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            float intensity = maxIntensity * Mathf.Clamp01(t / time);
            cardTransform.localPosition = cardRestPos + (Vector3)(Random.insideUnitCircle * intensity);
            yield return null;
        }
        cardTransform.localPosition = cardRestPos;
    }

    /// <summary>
    /// Replaces the card with two halves cut along the slash, they split apart, hang for a moment and fall off.
    /// Halves are created next to the unit (not inside it), so the discard shrink doesn't affect them
    /// </summary>
    public void CutInHalf()
    {
        cardCutter.Cut(card, transform);
    }

    /// <summary>
    /// Cleaning up halves from the previous cut and showing the whole card again
    /// </summary>
    void RemoveCutHalves()
    {
        cardCutter.RemoveHalves();
        card.gameObject.SetActive(true);
        card.transform.localPosition = cardRestPos;
        card.transform.localScale = cardRestScale;
    }
}
