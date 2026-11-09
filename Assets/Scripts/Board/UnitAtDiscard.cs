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
    [SerializeField] GameObject CutVFX;

    [Header("discard attributes")]
    [SerializeField] float discardedUnitSizeMod = 0.75f;

    [Header("cut in half")]
    [SerializeField] float slashAngle = 9.2f; // angle of the CutVFX slash, the cut line follows it
    [SerializeField] float cutAngleVariation = 15f;
    [SerializeField] float splitDistance = 18f; // how far the halves jump apart right after the cut
    [SerializeField] float slipDistance = 14f; // how far the halves slide along the cut line
    [SerializeField] float splitTime = 0.15f;
    [SerializeField] float holdTime = 0.35f; // halves hang in the air for a moment before falling
    [SerializeField] float fallTime = 0.8f;
    [SerializeField] float fallSpeed = 250f;
    [SerializeField] float fallGravity = 2200f;
    [SerializeField] float fallSpin = 160f;

    [HideInInspector] public Card storedCard;
    bool discarded = false;
    [HideInInspector] public bool lockInteraction = false;
    Vector3 defaultScale;
    Vector3 cardRestScale;
    Vector3 cardRestPos;
    List<GameObject> cutHalves = new List<GameObject>();

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

    void UnitDeathAnim()
    {
        // VFX
        CutVFX.SetActive(true);

        // SFX
        AudioManager.instance.PlaySFX("UnitDeathSFX");
    }

    /// <summary>
    /// Indicates that unit is discarded (or reverses it if false)
    /// </summary>
    /// <param name="isDiscarded"></param>
    public void Discard(bool isDiscarded)
    {
        discarded = isDiscarded;

        if (isDiscarded) UnitDeathAnim();
        else RemoveCutHalves();

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
    /// Replaces the card with two halves cut along the slash, they split apart, hang for a moment and fall off
    /// </summary>
    public void CutInHalf()
    {
        RemoveCutHalves();

        // the card might still have the hover highlight on, halves shouldn't have it
        card.HighlightCard(false);

        // picking a cut angle around the slash and turning the slash VFX to match it
        float angleOffset = Random.Range(-cutAngleVariation, cutAngleVariation);
        CutVFX.transform.localRotation = Quaternion.Euler(0f, 0f, angleOffset);
        float cutAngle = (slashAngle + angleOffset) * Mathf.Deg2Rad;
        Vector2 cutDirection = new Vector2(Mathf.Cos(cutAngle), Mathf.Sin(cutAngle));
        Vector2 cutNormal = new Vector2(-cutDirection.y, cutDirection.x);

        // halves are created next to the unit (not inside it), so the discard shrink doesn't affect them
        GameObject upperHalf = CreateHalf(cutNormal);
        GameObject lowerHalf = CreateHalf(-cutNormal);
        card.gameObject.SetActive(false);

        StartCoroutine(HalfFlyAnim(upperHalf.transform, cutNormal, cutDirection, 1f));
        StartCoroutine(HalfFlyAnim(lowerHalf.transform, -cutNormal, -cutDirection, -1f));
    }

    GameObject CreateHalf(Vector2 keptSide)
    {
        GameObject half = new GameObject("cutHalf", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform halfTransform = half.GetComponent<RectTransform>();
        halfTransform.SetParent(transform.parent, false);
        halfTransform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
        halfTransform.position = transform.position;
        halfTransform.rotation = transform.rotation;
        halfTransform.localScale = transform.localScale;
        half.GetComponent<CanvasGroup>().blocksRaycasts = false;

        // copying the whole card (only its looks) and cutting every graphic along the same line
        GameObject cardCopy = Instantiate(card.gameObject, halfTransform);
        Destroy(cardCopy.GetComponent<Card>());
        Destroy(cardCopy.GetComponent<CardInput>());
        Destroy(cardCopy.GetComponent<HoverEffect>());
        Vector2 linePoint = halfTransform.InverseTransformPoint(CutVFX.transform.position);
        foreach (Graphic layer in cardCopy.GetComponentsInChildren<Graphic>(true))
        {
            layer.raycastTarget = false;
            layer.gameObject.AddComponent<HalfPlaneClip>().SetLine(halfTransform, linePoint, keptSide);
        }

        cutHalves.Add(half);
        return half;
    }

    IEnumerator HalfFlyAnim(Transform half, Vector2 awayDirection, Vector2 slipDirection, float spinDirection)
    {
        Vector3 startingPos = half.localPosition;
        Quaternion startingRotation = half.localRotation;
        Vector3 splitOffset = (Vector3)(awayDirection * splitDistance + slipDirection * slipDistance);
        CanvasGroup canvasGroup = half.GetComponent<CanvasGroup>();

        // jumping apart
        float t = 0;
        while (t < splitTime)
        {
            if (half == null) yield break;
            t += Time.deltaTime;
            float coolT = 1 - Mathf.Pow(1 - Mathf.Clamp01(t / splitTime), 3);
            half.localPosition = startingPos + splitOffset * coolT;
            half.localRotation = startingRotation * Quaternion.Euler(0f, 0f, 8f * spinDirection * coolT);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        // falling down, spinning and fading out
        Vector3 velocity = (Vector3)(awayDirection * fallSpeed * 0.4f) + Vector3.up * fallSpeed;
        t = 0;
        while (t < fallTime)
        {
            if (half == null) yield break;
            t += Time.deltaTime;
            velocity.y -= fallGravity * Time.deltaTime;
            half.localPosition += velocity * Time.deltaTime;
            half.Rotate(0f, 0f, fallSpin * spinDirection * Time.deltaTime);
            canvasGroup.alpha = 1 - Mathf.Clamp01(t / fallTime);
            yield return null;
        }

        half.gameObject.SetActive(false);
    }

    /// <summary>
    /// Cleaning up halves from the previous cut and showing the whole card again
    /// </summary>
    void RemoveCutHalves()
    {
        foreach (GameObject half in cutHalves) if (half != null) Destroy(half);
        cutHalves.Clear();
        card.gameObject.SetActive(true);
        card.transform.localPosition = cardRestPos;
        card.transform.localScale = cardRestScale;
    }
}
