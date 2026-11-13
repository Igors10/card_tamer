using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Cuts a card in half: a slash plays over it, the card is replaced with two halves cut along the slash,
/// they split apart, hang for a moment and fall off (Scissor Rat discard, Beast destroying a card)
/// </summary>
public class CardCutter : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] GameObject CutVFX; // slash image, moved onto the cut card (has to be on the same canvas)

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

    readonly List<GameObject> cutHalves = new List<GameObject>();

    private void OnDisable()
    {
        RemoveHalves();
    }

    /// <summary>
    /// Cuts the card: the halves appear right after nextTo in its parent (so nextTo's scale changes don't affect them),
    /// the card itself gets hidden
    /// </summary>
    public void Cut(Card card, Transform nextTo)
    {
        RemoveHalves();
        Transform cardTransform = card.transform;

        // the card might still have the hover highlight on, halves shouldn't have it
        card.HighlightCard(false);

        // picking a cut angle around the slash and turning the slash VFX to match it (restarting its fade)
        float angleOffset = Random.Range(-cutAngleVariation, cutAngleVariation);
        CutVFX.transform.SetPositionAndRotation(cardTransform.position, cardTransform.rotation * Quaternion.Euler(0f, 0f, angleOffset));
        CutVFX.transform.SetAsLastSibling();
        CutVFX.SetActive(false);
        CutVFX.SetActive(true);
        AudioManager.instance.PlaySFX("UnitDeathSFX");

        float cutAngle = (slashAngle + angleOffset) * Mathf.Deg2Rad;
        Vector2 cutDirection = new Vector2(Mathf.Cos(cutAngle), Mathf.Sin(cutAngle));
        Vector2 cutNormal = new Vector2(-cutDirection.y, cutDirection.x);

        GameObject upperHalf = CreateHalf(card, nextTo, cutNormal);
        GameObject lowerHalf = CreateHalf(card, nextTo, -cutNormal);
        card.gameObject.SetActive(false);

        StartCoroutine(HalfFlyAnim(upperHalf.transform, cutNormal, cutDirection, 1f));
        StartCoroutine(HalfFlyAnim(lowerHalf.transform, -cutNormal, -cutDirection, -1f));
    }

    GameObject CreateHalf(Card card, Transform nextTo, Vector2 keptSide)
    {
        Transform cardTransform = card.transform;
        GameObject half = new GameObject("cutHalf", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform halfTransform = half.GetComponent<RectTransform>();
        halfTransform.SetParent(nextTo.parent, false);
        halfTransform.SetSiblingIndex(nextTo.GetSiblingIndex() + 1);
        halfTransform.SetPositionAndRotation(cardTransform.position, cardTransform.rotation);
        Vector3 parentScale = nextTo.parent.lossyScale;
        Vector3 cardScale = cardTransform.lossyScale;
        halfTransform.localScale = new Vector3(cardScale.x / parentScale.x, cardScale.y / parentScale.y, 1f);
        half.GetComponent<CanvasGroup>().blocksRaycasts = false;

        // copying the whole card (only its looks) and cutting every graphic along the same line
        GameObject cardCopy = Instantiate(card.gameObject, halfTransform);
        cardCopy.transform.localPosition = Vector3.zero;
        cardCopy.transform.localRotation = Quaternion.identity;
        cardCopy.transform.localScale = Vector3.one;
        Destroy(cardCopy.GetComponent<Card>());
        Destroy(cardCopy.GetComponent<CardInput>());
        Destroy(cardCopy.GetComponent<HoverEffect>());
        foreach (Graphic layer in cardCopy.GetComponentsInChildren<Graphic>(true))
        {
            layer.raycastTarget = false;
            layer.gameObject.AddComponent<HalfPlaneClip>().SetLine(halfTransform, Vector2.zero, keptSide);
        }

        cutHalves.Add(half);
        return half;
    }

    IEnumerator HalfFlyAnim(Transform half, Vector2 awayDirection, Vector2 slipDirection, float spinDirection)
    {
        // directions are along the card, turned into the parent's space (the card can be tilted)
        Vector3 away = half.localRotation * awayDirection;
        Vector3 slip = half.localRotation * slipDirection;
        Vector3 startingPos = half.localPosition;
        Quaternion startingRotation = half.localRotation;
        Vector3 splitOffset = away * splitDistance + slip * slipDistance;
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
        Vector3 velocity = away * fallSpeed * 0.4f + Vector3.up * fallSpeed;
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

        if (half != null) half.gameObject.SetActive(false);
    }

    /// <summary>
    /// Cleaning up halves from the previous cut
    /// </summary>
    public void RemoveHalves()
    {
        foreach (GameObject half in cutHalves) if (half != null) Destroy(half);
        cutHalves.Clear();
    }
}
