using UnityEditor;
using UnityEngine;

public class Field : MonoBehaviour
{
    public GameObject[] units = new GameObject[2];

    [Header("refs")]
    [SerializeField] SpriteRenderer spawnPoint;
    [SerializeField] SpriteRenderer sprite;

    [Header("Highlight")]
    Color defaultColor;
    Color defaultSpawnPointColor;
    [SerializeField] Color highlighColor;
    [SerializeField] Color highlightSpawnPointColor;
    bool cardIsOver;

    private void Start()
    {
        // getting the default colors
        defaultColor = sprite.color;
        defaultSpawnPointColor = spawnPoint.color;
    }

    /// <summary>
    /// Makes the field being available for playing cards on it
    /// </summary>
    /// <param name="enable"></param>
    public void EnableSpawnPoint(bool enable)
    {
        if (units[0] != null || units[1] != null) return;

        spawnPoint.gameObject.SetActive(enable);
    }

    private void Update()
    {
        IsCardOver();
    }

    /// <summary>
    /// Checks if there's a card being dragged over this field
    /// </summary>
    void IsCardOver()
    {
        if (!spawnPoint.gameObject.activeSelf) return; // only matters when a card is being dragged

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
        {
            if (cardIsOver == false) HighlightField(true); // highlighting field if not already
            cardIsOver = true;

            Debug.Log("Field: Mouse is over this field");
        }
        else
        {
            if (cardIsOver) HighlightField(false); // stop highlighting
            cardIsOver = false;
        }
    }

    void HighlightField(bool highlight)
    {
        sprite.color = (highlight) ? highlighColor : defaultColor;
        spawnPoint.color = (highlight) ? highlightSpawnPointColor : defaultSpawnPointColor;
    }

    /// <summary>
    /// Plays a creature on this field if a card is over it
    /// </summary>
    /// <param name="cardPlayed"></param>
    public void PlayCard(Card cardPlayed)
    {
        if (cardIsOver == false) return;

        // spawn a creature
        Debug.Log("Field: a " + cardPlayed.cardData.name + " has been spawned");
        HighlightField(false);
    }
}
