using UnityEditor;
using UnityEngine;

public class Field : MonoBehaviour
{
    public Unit[] units = new Unit[2];

    [Header("refs")]
    [SerializeField] SpriteRenderer spawnPoint;
    [SerializeField] SpriteRenderer sprite;
    public Transform[] unitSlots = new Transform[2];

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
    public void EnableSpawnSlot(int spawnSlot)
    {
        if (units[spawnSlot] != null) return;

        unitSlots[spawnSlot].gameObject.SetActive(true);
    }

    public void DisableSpawnSlots()
    {
        unitSlots[0].gameObject.SetActive(false);
        unitSlots[1].gameObject.SetActive(false);
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
        if (cardIsOver == false || units[1] != null) return;

        // spawn a creature
        GameManager.instance.fieldManager.SpawnUnit(cardPlayed, this);

        // moving card to field cards
        GameManager.instance.handManager.AddCardToField(cardPlayed);

        Debug.Log("Field: a " + cardPlayed.cardData.name + " has been spawned");
        HighlightField(false);

        // Ending the turn after playing a card if opponent hasn't finished putting their cards
        if (!GameManager.instance.opponentEndStateReady) GameManager.instance.EndTurn();
    }

    
}
