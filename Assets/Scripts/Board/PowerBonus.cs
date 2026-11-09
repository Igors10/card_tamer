using UnityEngine;
using System.Collections;
using TMPro;

public class PowerBonus : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] TextMeshProUGUI text;
    Coroutine currentBonus;
    Coroutine squashRoutine;
    int shownBonus; // bonuses that come in quickly one after another add up
    Vector2 restPos;
    Vector3 restScale;
    bool restSaved;
    bool outlineSet;

    [Header("looks")]
    [SerializeField] Color buffColor = Color.white;
    [SerializeField] Color debuffColor = new Color(0.94f, 0.25f, 0.25f);
    [SerializeField] Color buffOutlineColor = new Color(1f, 0.82f, 0.2f); // yellow, the buff color
    [SerializeField] Color outlineColor = Color.black; // debuffs
    [SerializeField] float outlineWidth = 0.25f;

    [Header("anim params")]
    [SerializeField] float popTime = 0.3f;
    [SerializeField] float popOvershoot = 2.2f; // how far the scale shoots past its size before settling
    [SerializeField] float travelDistance = 0.6f; // buffs fly up from the counter, debuffs slam down onto it
    [SerializeField] float comboScaleBonus = 0.15f; // every stacked bonus makes the number a bit bigger
    [SerializeField] float maxComboScale = 1.6f;
    [SerializeField] float timeBeforeFade = 0.6f;
    [SerializeField] float fadeTime = 0.35f;
    [SerializeField] float fadeDrift = 0.35f;

    void SaveRest()
    {
        if (restSaved) return;
        restPos = text.rectTransform.anchoredPosition;
        restScale = text.rectTransform.localScale;
        restSaved = true;
    }

    public void StartShowingBonus(int bonus)
    {
        if (bonus == 0 || !gameObject.activeInHierarchy) return;
        SaveRest();

        // stacking with the bonus that is still on screen if it goes the same way
        bool stacking = currentBonus != null && Mathf.Sign(shownBonus) == Mathf.Sign(bonus);
        int combo = stacking ? Mathf.Abs(shownBonus + bonus) : Mathf.Abs(bonus);
        shownBonus = stacking ? shownBonus + bonus : bonus;

        if (currentBonus != null) StopCoroutine(currentBonus);
        if (squashRoutine != null) StopCoroutine(squashRoutine);
        currentBonus = StartCoroutine(ShowBonus(shownBonus, combo));
    }

    /// <summary>
    /// Number pops out next to the power counter: buffs launch up, debuffs slam down, then it drifts away and fades
    /// </summary>
    IEnumerator ShowBonus(int bonus, int combo)
    {
        RectTransform rect = text.rectTransform;
        bool isBuff = bonus > 0;
        float dir = isBuff ? 1f : -1f;
        float comboScale = Mathf.Min(1f + comboScaleBonus * (combo - 1), maxComboScale);
        Vector3 targetScale = restScale * comboScale;

        // enabling text (no sway, it stays straight)
        text.text = isBuff ? "+" + bonus : bonus.ToString();
        Color color = isBuff ? buffColor : debuffColor;
        text.color = color;
        rect.localRotation = Quaternion.identity;
        text.gameObject.SetActive(true);

        // outline can only be set once the text has been active (TMP sets up its material then)
        if (!outlineSet)
        {
            text.outlineWidth = outlineWidth;
            outlineSet = true;
        }
        text.outlineColor = isBuff ? buffOutlineColor : outlineColor;

        // POP IN
        Vector2 startPos = restPos - new Vector2(0f, travelDistance * dir);
        float t = 0;
        while (t < popTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / popTime);

            // ease out back, overshoots the size and then settles
            float backT = 1 + (popOvershoot + 1) * Mathf.Pow(clampedT - 1, 3) + popOvershoot * Mathf.Pow(clampedT - 1, 2);
            float moveT = 1 - (1 - clampedT) * (1 - clampedT) * (1 - clampedT);

            rect.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, backT);
            rect.anchoredPosition = Vector2.Lerp(startPos, restPos, moveT);

            // white flash at the start
            text.color = Color.Lerp(Color.white, color, clampedT * 2f);
            yield return null;
        }
        rect.localScale = targetScale;
        rect.anchoredPosition = restPos;

        // debuffs land with a squash
        if (!isBuff) squashRoutine = StartCoroutine(Squash(rect, targetScale));

        // HOLD
        yield return new WaitForSeconds(timeBeforeFade);

        // FADE OUT, drifting the same way the power went
        rect.localScale = targetScale;
        Vector2 fadeStartPos = rect.anchoredPosition;
        Vector2 fadeEndPos = fadeStartPos + new Vector2(0f, fadeDrift * dir);
        t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / fadeTime);
            float coolT = clampedT * clampedT;

            rect.anchoredPosition = Vector2.Lerp(fadeStartPos, fadeEndPos, coolT);
            text.color = new Color(color.r, color.g, color.b, 1 - coolT);
            yield return null;
        }

        // resetting and deactivating text
        text.gameObject.SetActive(false);
        rect.anchoredPosition = restPos;
        rect.localScale = restScale;
        rect.localRotation = Quaternion.identity;
        shownBonus = 0;

        // removing reference to coroutine
        currentBonus = null;
    }

    IEnumerator Squash(RectTransform rect, Vector3 baseScale)
    {
        float squashTime = 0.18f;
        float t = 0;
        while (t < squashTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / squashTime);
            float squash = Mathf.Sin(clampedT * Mathf.PI) * 0.35f;
            rect.localScale = new Vector3(baseScale.x * (1 + squash), baseScale.y * (1 - squash), baseScale.z);
            yield return null;
        }
        rect.localScale = baseScale;
        squashRoutine = null;
    }
}
