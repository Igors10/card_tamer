using UnityEngine;

/// <summary>
/// Arrow from the attacking unit to the mouse while the player picks a target (or to the picked target when the opponent attacks)
/// </summary>
public class TargetArrow : MonoBehaviour
{
    [SerializeField] RectTransform line; // pivot on the left side, stretched to the end point
    [SerializeField] RectTransform head;
    [SerializeField] float headGap = 10f; // line stops a little before the head
    Transform from;
    Transform to; // UI object to point at, null = the mouse

    public void Show(Transform fromObject, Transform toObject = null)
    {
        from = fromObject;
        to = toObject;
        gameObject.SetActive(true);
        UpdateArrow();
    }

    public void Hide()
    {
        from = null;
        to = null;
        gameObject.SetActive(false);
    }

    void Update()
    {
        UpdateArrow();
    }

    void UpdateArrow()
    {
        if (from == null) return;

        Vector3 start = Camera.main.WorldToScreenPoint(from.position);
        start.z = 0f;
        Vector3 end = (to != null) ? to.position : Input.mousePosition;
        end.z = 0f;
        Vector3 direction = end - start;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float scale = line.lossyScale.x;

        line.position = start;
        line.rotation = Quaternion.Euler(0f, 0f, angle);
        line.sizeDelta = new Vector2(Mathf.Max(0f, direction.magnitude / scale - headGap), line.sizeDelta.y);

        head.position = end;
        head.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
