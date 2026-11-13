using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One of the player's hearts in the player UI. A lost heart disappears (its copy breaks, see Break)
/// </summary>
public class PlayerHeart : MonoBehaviour
{
    [SerializeField] GameObject fill;
    [SerializeField] Image outline;

    [Header("Break animation")]
    [SerializeField] float hoverHeight = 60f; // the whole heart lifts out of its slot first
    [SerializeField] float hoverTime = 0.4f;
    [SerializeField] float breakTime = 0.8f;
    [SerializeField] float breakGap = 12f; // the halves first crack apart this much
    [SerializeField] float breakSpeedX = 120f; // then fly sideways...
    [SerializeField] float breakJump = 250f; // ...hop up...
    [SerializeField] float breakGravity = 1200f; // ...and fall down
    [SerializeField] float breakSpin = 60f; // degrees per second, each half tilts outwards
    [SerializeField] float outlineFadeTime = 0.5f;

    public bool IsFull => fill.activeSelf;

    public void SetFull(bool full)
    {
        fill.SetActive(full);
        outline.gameObject.SetActive(full);
    }

    /// <summary>
    /// A full copy of the heart hovers up out of its (now empty) slot, its inside shatters in two and its outline fades away.
    /// Done with UI images, so it shows over the rest of the UI
    /// </summary>
    public void Break()
    {
        PlayerHeart copy = Instantiate(this, transform.parent);
        copy.transform.SetAsLastSibling();
        copy.SetFull(true);
        copy.StartCoroutine(copy.BreakAnim());
    }

    IEnumerator BreakAnim()
    {
        // HOVER: the whole heart lifts up
        RectTransform rect = (RectTransform)transform;
        Vector2 slot = rect.anchoredPosition;
        float t = 0;
        while (t < hoverTime)
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / hoverTime);
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);
            rect.anchoredPosition = slot + new Vector2(0f, hoverHeight * coolT);
            yield return null;
        }

        // SHATTER: the fill is a horizontally filled image, so each half is a copy filled from its own side
        Image left = MakeHalf(Image.OriginHorizontal.Left);
        Image right = MakeHalf(Image.OriginHorizontal.Right);
        fill.SetActive(false);
        AudioManager.instance.PlaySFX("HitSFX");
        Vector2 start = ((RectTransform)fill.transform).anchoredPosition;

        // the halves fly apart while the outline fades away
        Color outlineColor = outline.color;
        t = 0;
        while (t < Mathf.Max(breakTime, outlineFadeTime))
        {
            t += Time.deltaTime;
            float clampedT = Mathf.Clamp01(t / breakTime);
            float x = breakGap + breakSpeedX * t;
            float y = breakJump * t - breakGravity * t * t / 2f;
            float alpha = 1f - clampedT * clampedT;

            MoveHalf(left, start + new Vector2(-x, y), breakSpin * t, alpha);
            MoveHalf(right, start + new Vector2(x, y), -breakSpin * t, alpha);

            outlineColor.a = 1f - Mathf.Clamp01(t / outlineFadeTime);
            outline.color = outlineColor;
            yield return null;
        }

        Destroy(gameObject);
    }

    Image MakeHalf(Image.OriginHorizontal side)
    {
        Image half = Instantiate(fill, transform).GetComponent<Image>();
        half.transform.SetAsLastSibling();
        half.raycastTarget = false;
        half.fillOrigin = (int)side;
        half.fillAmount = 0.5f;
        return half;
    }

    void MoveHalf(Image half, Vector2 position, float angle, float alpha)
    {
        ((RectTransform)half.transform).anchoredPosition = position;
        half.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        Color color = half.color;
        color.a = alpha;
        half.color = color;
    }
}
