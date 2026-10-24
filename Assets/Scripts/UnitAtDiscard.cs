using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class UnitAtDiscard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material selectMaterial;
    [SerializeField] Material discardMaterial;
    public UnitSprite sprite;
    [SerializeField] Image[] unitPiece;
    [SerializeField] CartoonShakeEffect cartoonShakeEffect;
    [SerializeField] HoverEffect hoverEffect;
    [SerializeField] GameObject CutVFX;

    [Header("discard attributes")]
    [SerializeField] Color discardedUnitColor;
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

    [Header("flinching")]
    [SerializeField] float flinchSquash = 0.2f;
    [SerializeField] float flinchTime = 0.25f;

    [HideInInspector] public Card storedCard;
    bool discarded = false;
    [HideInInspector] public bool lockInteraction = false;
    Vector3 defaultScale;
    Vector3 spriteRestScale;
    Vector3 spriteRestPos;
    Coroutine flinchRoutine;
    List<GameObject> cutHalves = new List<GameObject>();

    private void Awake()
    {
        defaultScale = transform.localScale;
        spriteRestScale = sprite.transform.localScale;
        spriteRestPos = sprite.transform.localPosition;
    }

    private void OnDisable()
    {
        // halves live next to the unit, so they have to be cleaned up when the unit gets hidden
        RemoveCutHalves();
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

        // moving discard pointer over it 
        GameManager.instance.discardManager.discardPointer.SetActive(isHover);
        GameManager.instance.discardManager.MovePointer(transform.position);

        // color the unit as selectable
        Material newMaterial = (isHover && GameManager.instance.discardManager.discardAvailable) ? selectMaterial : defaultMaterial;
        sprite.RefreshSpriteMaterial(newMaterial);

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

        // Making unit appear cut in half
        /*
        for (int i = 0; i < unitPiece.Length; i++)
        {
            unitPiece[i].gameObject.SetActive(isDiscarded);
            //unitPiece[i].sprite = sprite.sprite;
        }*/

        // Disabling the preview
        GameManager.instance.managerUI.PreviewCard(false, storedCard.cardData, storedCard.player, Vector3.zero);

        // chaning the material
        Material material = (isDiscarded) ? discardMaterial : defaultMaterial; 
        sprite.RefreshSpriteMaterial(material);

        // changing the color
        Color primColor = (isDiscarded) ? discardedUnitColor : storedCard.player.playerColor;
        Color secColor = (isDiscarded) ? discardedUnitColor : storedCard.cardData.secondaryColor;
        sprite.RefreshColor(primColor, secColor);

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
    /// Quick squash when the scissor rat's pointer lands on the unit
    /// </summary>
    public void Flinch()
    {
        if (flinchRoutine != null) StopCoroutine(flinchRoutine);
        flinchRoutine = StartCoroutine(Animations.instance.SquashStretch(sprite.transform, spriteRestScale, flinchSquash, flinchTime));
    }

    /// <summary>
    /// Unit shakes harder and harder right before getting cut
    /// </summary>
    public IEnumerator Tremble(float time, float maxIntensity)
    {
        Transform spriteTransform = sprite.transform;
        float t = 0;
        while (t < time)
        {
            t += Time.deltaTime;
            float intensity = maxIntensity * Mathf.Clamp01(t / time);
            spriteTransform.localPosition = spriteRestPos + (Vector3)(Random.insideUnitCircle * intensity);
            yield return null;
        }
        spriteTransform.localPosition = spriteRestPos;
    }

    /// <summary>
    /// Replaces the unit with two halves cut along the slash, they split apart, hang for a moment and fall off
    /// </summary>
    public void CutInHalf()
    {
        RemoveCutHalves();

        // the unit might still have the hover highlight on, halves should have the normal outline
        sprite.RefreshSpriteMaterial(defaultMaterial);

        // picking a cut angle around the slash and turning the slash VFX to match it
        float angleOffset = Random.Range(-cutAngleVariation, cutAngleVariation);
        CutVFX.transform.localRotation = Quaternion.Euler(0f, 0f, angleOffset);
        float cutAngle = (slashAngle + angleOffset) * Mathf.Deg2Rad;
        Vector2 cutDirection = new Vector2(Mathf.Cos(cutAngle), Mathf.Sin(cutAngle));
        Vector2 cutNormal = new Vector2(-cutDirection.y, cutDirection.x);

        // halves are created next to the unit (not inside it), so the discard shrink doesn't affect them
        GameObject upperHalf = CreateHalf(cutNormal);
        GameObject lowerHalf = CreateHalf(-cutNormal);
        sprite.gameObject.SetActive(false);

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

        // copying the whole layered sprite (colors and outline materials) and cutting every layer along the same line
        GameObject spriteCopy = Instantiate(sprite.gameObject, halfTransform);
        Vector2 linePoint = halfTransform.InverseTransformPoint(CutVFX.transform.position);
        foreach (Image layer in spriteCopy.GetComponentsInChildren<Image>(true))
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
    /// Cleaning up halves from the previous cut and showing the whole unit again
    /// </summary>
    void RemoveCutHalves()
    {
        foreach (GameObject half in cutHalves) if (half != null) Destroy(half);
        cutHalves.Clear();
        sprite.gameObject.SetActive(true);
        sprite.transform.localPosition = spriteRestPos;
        sprite.transform.localScale = spriteRestScale;
    }
}
