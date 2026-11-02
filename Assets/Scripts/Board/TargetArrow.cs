using UnityEngine;

/// <summary>
/// Arrow from the attacking unit to the mouse while the player picks a target
/// </summary>
public class TargetArrow : MonoBehaviour
{
    [SerializeField] RectTransform line; // pivot on the left side, stretched to the mouse
    [SerializeField] RectTransform head;
    [SerializeField] float headGap = 10f; // line stops a little before the head
    Transform from;

    public void Show(Transform fromObject)
    {
        from = fromObject;
        gameObject.SetActive(true);
        UpdateArrow();
    }

    public void Hide()
    {
        from = null;
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
        Vector3 end = Input.mousePosition;
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
