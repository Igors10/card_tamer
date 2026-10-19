using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscardManager : MonoBehaviour
{
    [Header("animation times")]
    [SerializeField] float ratThinkingTime;
    [SerializeField] float beforeDiscardTime;
    [SerializeField] float afterDiscardTime;
    [SerializeField] float unitsMovingTime;
    [SerializeField] float pointerInterval;

    [Header("refs")]
    [SerializeField] DeathAnim death;
    [SerializeField] UnitAtDiscard[] units;
    public GameObject previewPoint;
    [SerializeField] GameObject discardUI;
    public GameObject discardPointer;
    [SerializeField] Button closeButton;
    [SerializeField] TextMeshProUGUI discardText;

    List<UnitAtDiscard> currentUnits = new List<UnitAtDiscard>();
    bool currentDiscardRandom = true;
    [HideInInspector] public bool discardAvailable = false;
    bool discarding = false;

    private void Start()
    {
        // starting the unit discard
        GameManager.instance.discardManager = this;
    }

    public IEnumerator StartDiscardSequence(Player player, bool randomDiscard)
    {
        // little pause before enabling the discards
        if (randomDiscard) yield return new WaitForSeconds(beforeDiscardTime);

        discarding = true;
        currentDiscardRandom = randomDiscard;

        // Enabling the discard button if the discard is not random
        closeButton.gameObject.SetActive(!randomDiscard);

        // discard player units
        List<Card> listOfCardsForDiscard = (randomDiscard) ? player.cardsInDiscard : player.cardsInHand;
        AssignDiscardedUnits(listOfCardsForDiscard, player, randomDiscard);

        // Changing workshop hint
        string hintMessage = (randomDiscard) ? "Scissor Rat destroys one knocked card" : "Choose a card to destroy for 1 coin";
        discardText.text = hintMessage;

        if (randomDiscard && GameManager.instance.player.cardsInDiscard.Count == 0) StartCoroutine(FinishDiscard());
        else if (randomDiscard) StartCoroutine(AutoDiscard(player));

        // not finishing the coroutine before the discard is finished so that the battle phase doesnt change for the shop
        while (discarding) { yield return null; }
    }

    /// <summary>
    /// Makes discarded units of chosen player appear on the discard UI
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    void AssignDiscardedUnits(List<Card> listOfCards, Player player, bool lockHoveringOver)
    {
        // deactivating units 
        foreach (UnitAtDiscard unit in units) unit.gameObject.SetActive(false);

        // if no units lost then skip discard for this player
        if (listOfCards.Count < 1)
        {
            StartCoroutine(FinishDiscard());
            return;
        }

        // enabling the discard available
        discardUI.SetActive(true);
        discardAvailable = true;

        // INITIALYZING UNITS
        List<Vector3> unitDiscardPos = new List<Vector3>();        

        for (int i = 0; i < listOfCards.Count; i++)
        {
            // activating units
            units[i].gameObject.SetActive(true);
            units[i].sprite.RefreshSprite(listOfCards[i].cardData.unitSprite, player.playerColor, listOfCards[i].cardData.secondaryColor);
            units[i].storedCard = listOfCards[i];
            units[i].Discard(false);
            units[i].lockInteraction = lockHoveringOver;
            currentUnits.Add(units[i]);

            // assigning units to a position
            Vector3 newUnitPosition = units[i].transform.position + new Vector3(Random.Range(-40, 40), Random.Range(-40, 40), 0f);
            unitDiscardPos.Add(newUnitPosition);
        }
    }

    /// <summary>
    /// If player decides not to discard a card, this closes the discard UI
    /// </summary>
    public void CloseDiscardWindow()
    {
        StartCoroutine(FinishDiscard());
    }

    IEnumerator FinishDiscard()
    {
        // pause to let all the anims play out and let player understand which card was discarded
        if (currentDiscardRandom) yield return new WaitForSeconds(afterDiscardTime);

        // Animating the discard UI disappearance
        float fadeOutTime = afterDiscardTime / 3;
        if (discardUI.activeSelf && currentDiscardRandom)
        {
            discardUI.GetComponent<AutoFade>().FadeOut(fadeOutTime);
            yield return new WaitForSeconds(fadeOutTime);
        }

        // resetting discard vals
        currentUnits.Clear();
        discardAvailable = false;
        discarding = false;

        discardPointer.SetActive(false);
        discardUI.SetActive(false);
        GameManager.instance.shopManager.cardDisarded = true;

        // refreshin the shop (to see if the discard button still available)
        GameManager.instance.shopManager.Refresh();
        //GameManager.instance.managerUI.workshop.ChangeHint("Create new cards!");
    }

    public void MovePointer(Vector3 unitPos, bool randomUnitPos = false)
    {
        if (currentUnits.Count < 1) return;

        // enabling the pointer
        discardPointer.SetActive(true);
        
        // moving the pointer to a random different unit
        Vector3 newPointerPosition = unitPos;

        if (randomUnitPos) // choosing a random position 
        {
            do
            {
                newPointerPosition = currentUnits[Random.Range(0, currentUnits.Count)].transform.position;
            } while (newPointerPosition == discardPointer.transform.position && currentUnits.Count != 1); // making sure the new chosen unit is different from the prev one
        }
        discardPointer.transform.position = newPointerPosition;

        // Playing SFX
        if (currentUnits.Count > 1) AudioManager.instance.PlaySFX("RerollSFX");
    }

    /// <summary>
        /// Lets the game choose discarded units for the player;
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
    public IEnumerator AutoDiscard(Player player)
    {
        yield return Animations.instance.SkippablePause(beforeDiscardTime);
        Debug.Log("DiscardManager: auto discard started");


        // Pointer randomly moving around simulating rat choosing a target
        float t = 0;
        float pointerT = 0;

        while (t < ratThinkingTime)
        {
            t += Time.deltaTime;
            pointerT += Time.deltaTime;

            // jumping the pointer to another unit when interval time is reached
            if (pointerT >= pointerInterval)
            {
                pointerT = 0;
                MovePointer(Vector3.zero, true);
            }
            yield return null;
        }


        // choosing random unit and discarding it
        int randomUnitID = Random.Range(0, currentUnits.Count);
        MovePointer(currentUnits[randomUnitID].transform.position);
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
        discardText.text = newHintMessage;

        // removing the card from game
        unit.storedCard.DestroyCard();

        GameManager.instance.handManager.UpdateHandVisuals(GameManager.instance.player);
        StartCoroutine(FinishDiscard());
    }
}
