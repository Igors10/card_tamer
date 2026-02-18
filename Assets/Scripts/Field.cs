using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Field : MonoBehaviour
{
    public Unit[] units = new Unit[2];

    [Header("refs")]
    [SerializeField] SpriteRenderer spawnPoint;
    public SpriteRenderer sprite;
    public Transform[] unitSlots = new Transform[2];
    [SerializeField] GameObject fieldUI;

    [Header("Block")]
    [HideInInspector] public GameObject blockObj;
    [SerializeField] TextMeshProUGUI blockValue;
    [HideInInspector] public int currentBlock;
    [SerializeField] bool mirrorBlock;

    [Header("Highlight")]
    Color defaultColor;
    Color defaultSpawnPointColor;
    [SerializeField] Color highlighColor; // used for highlighting tiles that are available for spawning
    [SerializeField] Color dimHighlightColor; // used for highlighting tiles that are available for moving
    [SerializeField] Color highlightSpawnPointColor;
    [SerializeField] Color fadedColor;
    bool cardIsOver;

    private void Start()
    {
        // getting the default colors
        defaultColor = sprite.color;
        defaultSpawnPointColor = spawnPoint.color;

        // Mirroring block if enabled
        if (mirrorBlock) blockObj.transform.localPosition = new Vector2(-blockObj.transform.localPosition.x, blockObj.transform.localPosition.y);
    }

    /// <summary>
    /// Makes the field being available for playing cards on it
    /// </summary>
    /// <param name="enable"></param>
    public void EnableSpawnSlot(int spawnSlot = 0) 
    {
        // for moving units it first checks the front slot
        if (spawnSlot == 0 && units[spawnSlot] != null)
        { spawnSlot = 1; } // checking the backslot if front is taken

        if (spawnSlot == 1 && units[spawnSlot] != null) return; // both slots (or back one for spawning) are not available 

        // activating needed slot
        unitSlots[spawnSlot].gameObject.SetActive(true);
    }

    public void DisableAllSlots()
    {
        unitSlots[0].gameObject.SetActive(false);
        unitSlots[1].gameObject.SetActive(false);
        MoveHighlightField(false);
    }

    /// <summary>
    /// Making the field and its units fade out a little
    /// </summary>
    /// <param name="isFadeOut"></param>
    public void FadeOut(bool isFadeOut)
    {
        sprite.color = (isFadeOut) ? fadedColor : defaultColor;
        fieldUI.SetActive(!isFadeOut);

        // fading units
        foreach (Unit unit in units)
        {
             if (unit != null) { unit.faded = isFadeOut; unit.RefreshUnitVisuals(); }
        }
    }

    public List<Unit> GetFieldUnits()
    {
        List<Unit> unitsToReturn = new List<Unit>();

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null) unitsToReturn.Add(units[i]);
        }

        return unitsToReturn;
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
        if (!unitSlots[1].gameObject.activeSelf || GameManager.instance.currentState != GameState.PLACING) return; // only matters when a card is being dragged during placing state

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

    void HighlightField(bool highlight) // highlights the field when spawning cards from hand
    {
        sprite.color = (highlight) ? highlighColor : defaultColor;
        spawnPoint.color = (highlight) ? highlightSpawnPointColor : defaultSpawnPointColor;
    }

    public void MoveHighlightField(bool highlight) // highlights the field when moving units 
    {
        sprite.color = (highlight) ? dimHighlightColor : defaultColor;
    }

    /// <summary>
    /// Plays a creature on this field if a card is over it, returns true if a card was played on this field
    /// </summary>
    /// <param name="cardPlayed"></param>
    public bool PlayCard(Card cardPlayed, Player player)
    {
        if (cardIsOver == false || units[1] != null) return false;

        // spawn a creature
        GameManager.instance.fieldManager.SpawnUnit(cardPlayed, this);

        // moving card to field cards
        GameManager.instance.handManager.AddCardToField(cardPlayed, player);

        Debug.Log("Field: a " + cardPlayed.cardData.name + " has been spawned");
        HighlightField(false);

        // Returns true if the unit was spawned
        return true;
    }

    public void RefreshFieldVisuals()
    {
        if (currentBlock > 0)
        {
            blockObj.SetActive(true);
            blockValue.text = currentBlock.ToString();
        }
        else blockObj.SetActive(false);
    }

    /// <summary>
    /// Adds block to this field
    /// </summary>
    /// <param name="block"></param>
    public void GainBlock(int block)
    {
        currentBlock += block;
        RefreshFieldVisuals();
    }

    /// <summary>
    /// Resets all temporary field attributes (like block) at the end of round
    /// </summary>
    void FieldEndRound()
    {
        currentBlock = 0;
        RefreshFieldVisuals();
    }
}
