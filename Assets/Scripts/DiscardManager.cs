using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscardManager : MonoBehaviour
{
    [Header("animation times")]
    [SerializeField] float ratPointingTime;
    [SerializeField] float beforeDiscardTime;
    [SerializeField] float afterDiscardTime;
    [SerializeField] float unitsMovingTime;

    [Header("refs")]
    [SerializeField] DeathAnim death;
    [SerializeField] UnitAtDiscard[] units;
    public GameObject previewPoint;
    [SerializeField] GameObject discardUI;

    List<UnitAtDiscard> currentUnits = new List<UnitAtDiscard>();
    bool currentDiscardRandom = true;
    [HideInInspector] public bool discardAvailable = false;

    private void Start()
    {
        // starting the unit discard
        GameManager.instance.discardManager = this;
    }

    public void StartDiscardSequence(Player player, bool randomDiscard)
    {
        // Little juice effect for the whole discard window
        Animations.instance.PopAnim(discardUI, 0.25f, 0.3f);

        discardUI.SetActive(true);
        currentDiscardRandom = randomDiscard;

        // discard player units
        List<Card> listOfCardsForDiscard = (randomDiscard) ? player.cardsInDiscard : player.cardsInHand;
        AssignDiscardedUnits(listOfCardsForDiscard, player, randomDiscard);

        // Changing workshop hint
        string hintMessage = (randomDiscard) ? "Rat Wicked destroys one knocked card" : "Choose a card to destroy for 1 coin";
        GameManager.instance.managerUI.workshop.ChangeHint(hintMessage);

        if (randomDiscard && GameManager.instance.player.cardsInDiscard.Count == 0) StartCoroutine(FinishDiscard());
        else if (randomDiscard) StartCoroutine(AutoDiscard(player));
    }

    /// <summary>
    /// Makes discarded units of chosen player appear on the discard UI
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    void AssignDiscardedUnits(List<Card> listOfCards, Player player, bool lockHoveringOver)
    {
        // if no units lost then skip discard for this player
        if (listOfCards.Count < 1)
        {
            StartCoroutine(FinishDiscard());
            return;
        }            

        // making the discard available
        discardAvailable = true;

        // INITIALYZING UNITS
        List<Vector3> unitDiscardPos = new List<Vector3>();

        // deactivating units 
        foreach (UnitAtDiscard unit in currentUnits) unit.gameObject.SetActive(false);

        for (int i = 0; i < listOfCards.Count; i++)
        {
            // activating units
            units[i].gameObject.SetActive(true);
            units[i].sprite.RefreshSprite(listOfCards[i].cardData.unitSprite, player.playerColor, listOfCards[i].cardData.secondaryColor);
            units[i].storedCard = player.cardsInDiscard[i];
            units[i].Discard(false);
            units[i].lockHovering = lockHoveringOver;
            currentUnits.Add(units[i]);

            // assigning units to a position
            Vector3 newUnitPosition = units[i].transform.position + new Vector3(Random.Range(-40, 40), Random.Range(-40, 40), 0f);
            unitDiscardPos.Add(newUnitPosition);
        }
    }

    IEnumerator FinishDiscard()
    {
        // pause to let all the anims play out and let player understand which card was discarded
        //yield return Animations.instance.SkippablePause(afterDiscardTime);
        yield return new WaitForSeconds(afterDiscardTime);

        // deactivating units 
        //foreach (UnitAtDiscard unit in currentUnits) unit.gameObject.SetActive(false);

        // resetting discard vals
        currentUnits.Clear();
        discardAvailable = false;

        discardUI.SetActive(false);
        GameManager.instance.shopManager.cardDisarded = true;
    }

    /// <summary>
        /// Lets the game choose discarded units for the player;
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
    public IEnumerator AutoDiscard(Player player)
    {
        //yield return Animations.instance.SkippablePause(beforeDiscardTime);
        yield return new WaitForSeconds(beforeDiscardTime);
        Debug.Log("DiscardManager: auto discard started");

        // choosing random unit and discarding it
        int randomUnitID = Random.Range(0, currentUnits.Count);
        DiscardUnit(currentUnits[randomUnitID].GetComponentInParent<UnitAtDiscard>());
    }

    public void DiscardUnit(UnitAtDiscard unit)
    {
        Debug.Log("DiscardManager: unit discarded");

        // dead unit animation
        unit.Discard(true);

        // making death point finger at the unit 
        StartCoroutine(death.PointAnim(afterDiscardTime / 2));
        // displaying destroyed units name
        string newHintMessage = unit.storedCard.cardData.name + " is destroyed";
        if (!currentDiscardRandom) newHintMessage += " for +1 coin";
        GameManager.instance.managerUI.workshop.ChangeHint(newHintMessage);

        // removing the card from game
        unit.storedCard.DestroyCard();

        GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);
        StartCoroutine(FinishDiscard());
    }
}
