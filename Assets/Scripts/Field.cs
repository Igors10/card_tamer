using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Field : MonoBehaviour
{
    public Unit[] units = new Unit[2];
    [SerializeField] bool spawnable;

    [Header("refs")]
    [SerializeField] SpriteRenderer spawnPoint;
    public SpriteRenderer sprite;
    public Transform[] unitSlots = new Transform[2];
    [SerializeField] GameObject fieldUI;

    [Header("blocked")]
    [HideInInspector] public int roundsBlocked;
    [SerializeField] TextMeshProUGUI[] blockedNumber;

    [Header("Highlight")]
    [HideInInspector] public Color defaultColor;
    [SerializeField] Color highlighColor; // used for highlighting tiles that are available for spawning
    [SerializeField] Color dimHighlightColor; // used for highlighting tiles that are available for moving
    [SerializeField] Color highlightSpawnPointColor;
    [SerializeField] Color fadedColor;
    Material defaultMaterial;
    [SerializeField] Material selectMaterial;
    bool cardIsOver;

    private void Start()
    {
        // getting the default colors
        defaultColor = sprite.color;
        
        // getting default material
        defaultMaterial = sprite.material;
    }

    /// <summary>
    /// Makes the field being available for playing cards on it
    /// </summary>
    /// <param name="enable"></param>
    public void EnableSpawnSlot(int spawnSlot = 0)
    {
        // checking if field is blocked
        if (roundsBlocked > 0) return;

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

    public List<Unit> GetFieldUnits(bool onlyNotStunned = false)
    {
        List<Unit> unitsToReturn = new List<Unit>();

        for (int i = 0; i < units.Length; i++)
        {
            if (units[i] != null) unitsToReturn.Add(units[i]);
        }

        return unitsToReturn;
    }

    /// <summary>
    /// Returns combined power of all units on this field
    /// </summary>
    /// <returns></returns>
    public int GetFieldPower()
    {
        int combinedPower = 0;

        for (int i = 0; i < units.Length;i++)
        {
            if (units[i] == null) continue;
            combinedPower += units[i].card.currentPower;
        }

        return combinedPower;
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
        if (GameManager.instance.handManager.activeCard == null || GameManager.instance.currentState != GameState.PLACING
            || (units[0] != null && units[1] != null || GameManager.instance.executeManager.currentCard != null) || !spawnable 
            || !GameManager.instance.yourTurn || roundsBlocked > 0) return;

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
        sprite.material = (highlight) ? selectMaterial : defaultMaterial;
        //sprite.color = (highlight) ? highlighColor : defaultColor;
        //spawnPoint.color = (highlight) ? highlightSpawnPointColor : defaultSpawnPointColor;
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
        if ((player == GameManager.instance.player) && (cardIsOver == false || units[1] != null)
            || GameManager.instance.executeManager.currentCard != null && roundsBlocked == 0) return false;

        if (!cardPlayed.SpecialCost()) return false;

        // spawn a creature
        GameManager.instance.fieldManager.SpawnUnit(cardPlayed, this);

        // activating cards ability
        cardPlayed.PlayCard();

        Debug.Log("Field: a " + cardPlayed.cardData.name + " has been spawned");
        HighlightField(false);

        // Returns true if the unit was spawned
        return true;
    }

    public void RefreshFieldVisuals()
    {
        // applying color
        sprite.color = defaultColor;

        // blocked visuals
        for (int i = 0; i < units.Length; i++)
        {
            blockedNumber[i].gameObject.SetActive(units[i] == null && roundsBlocked > 0);
            if (blockedNumber[i].gameObject.activeSelf) blockedNumber[i].text = roundsBlocked.ToString();
        }
    }

   
    /// <summary>
    /// Resets all temporary field attributes (like block) at the end of round
    /// </summary>
    public void FieldEndRound()
    {
        roundsBlocked = 0;
        RefreshFieldVisuals();

        // fadeout cancel
        FadeOut(false);

        // reset field units
        foreach (Unit unit in units)
        {
            if (unit != null) unit.card.CardEndRound();
        }
    }

    public void FieldEndTurn()
    {
        // decrease field block
        if (roundsBlocked > 0) roundsBlocked--;

        RefreshFieldVisuals();
    }
}
