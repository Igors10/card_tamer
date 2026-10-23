using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecuteManager : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] NextCardButton nextCardButton;
    [SerializeField] Transform nextCardPos;
    [SerializeField] Transform revealedCardPos;

    // cards
    [HideInInspector] public List<string> playedCardNames = new List<string>();
    [HideInInspector] public Card currentCard;


    [Header("revealed card params")]
    [HideInInspector] public bool readyRevealCard = false;
    Vector3 revealedCardScale = new Vector3(1.8f, 1.8f, 1f);

    [Header("card play params")]
    [SerializeField] float zoomIntensity;
    public float zoomTime;

    [Header("Effects")]
    public List<IEnumerator> effectStack = new List<IEnumerator>();
    public List<IEnumerator> rollEffectStack = new List<IEnumerator>();
    public List<IEnumerator> battleOverEffectStack = new List<IEnumerator>();
    public Unit effectTarget;
    public AbilityRunner abilityRunner { get; private set; }

    private void OnEnable()
    {
        // new ability system listens to game events and fills the effect stacks
        if (abilityRunner == null) abilityRunner = new AbilityRunner(this);
        abilityRunner.Subscribe();
    }

    private void OnDisable()
    {
        abilityRunner.Unsubscribe();
    }

    public void RevealCard(Card cardToReveal)
    {
        currentCard = cardToReveal;
        currentCard.gameObject.SetActive(true);
        //StartCoroutine(currentCard.GetComponent<AutoFade>().FadeEffect(revealFadeInTime, 0f));

        // playing soundeffect
        AudioManager.instance.PlaySFX("NextCardSFX");

        // positioning the card
        currentCard.transform.position = revealedCardPos.position;
        currentCard.transform.localScale = revealedCardScale;

        // making card's abilities be ready to be clicked on
        if (GameManager.instance.yourTurn) currentCard.ActivateAbilities();
        // if its not player's turn mirror the card to appear on the opponents side of the screen
        else 
        {
            Camera camera = Camera.main;
            float rightSideX = camera.ViewportToWorldPoint(new Vector3(1, 1, camera.nearClipPlane)).x;
            currentCard.transform.localPosition = new Vector2(rightSideX - currentCard.transform.localPosition.x, currentCard.transform.localPosition.y);
            
        }
    }

    public void StopRevealCard()
    {
        // deactivate card
        currentCard.gameObject.SetActive(false);
        //StartCoroutine(currentCard.GetComponent<AutoFade>().FadeEffect(0f, revealFadeOutTime));
        currentCard.transform.localScale = currentCard.defaultScale;

        // reset current card var
        currentCard = null;

        // refresh fields
        GameManager.instance.fieldManager.Refresh();
    }

    public void CardUseAbl(Card card)
    {
        StartCoroutine(CardUseAbility(card));
    }
    /// <summary>
    /// Triggers all necessary animations and switches after an ability is used
    /// </summary>
    /// <param name="card"></param>
    /// <param name="ability"></param>
    /// <returns></returns>
    public IEnumerator CardUseAbility(Card card)
    {
        // checking if the card being played first time this game
        bool playedFirstTime = !playedCardNames.Contains(card.cardData.name);
        // add to the played card list
        if (playedFirstTime) playedCardNames.Add(card.cardData.name);
        bool showcase = playedFirstTime && card.unit.FirstSpawnShowcase;

        // marking that a player played a card
        card.player.cardPlayed = true;

        // zooming in on the unit
        if (showcase)
        {
            GameManager.instance.mainCamera.ZoomIn(card.unit.gameObject, zoomIntensity, zoomTime);
            yield return StartCoroutine(Animations.instance.SkippablePause(zoomTime));

            // enabling presenter
            card.unit.unitPresenter.gameObject.SetActive(true);
        }
        
        // Playing unit animation
        yield return StartCoroutine(card.unit.EntranceAnimation());

        // Deactivating the card
        GameManager.instance.executeManager.StopRevealCard();

        // zooming out from the unit
        if (showcase)
        {
            GameManager.instance.mainCamera.ZoomOut();
            yield return Animations.instance.SkippablePause(zoomTime);
        }

        // Card effect + power
        //yield return StartCoroutine(card.unit.RollPower());
        card.GetStartingPower();

        // Triggering on play card effects
        GameManager.instance.BroadcastOnCardPlayed(card);

        // waiting until all effects are resolved
        yield return StartCoroutine(TriggerEffects());

        // the card's starting power is final now (base or rolled), so effects reacting to it can trigger
        GameManager.instance.BroadcastOnCardPlayResolved(card);
        yield return StartCoroutine(TriggerEffects());

        // refreshing field colors
        GameManager.instance.fieldManager.Refresh();

        // ending the turn 
        card.player.EndTurn();
    }

    // ** EFFECTS STUFF **
    // -------------------

    /// <summary>
    /// Highlights certain units and waits for the player to choose one of them as a target
    /// </summary>
    /// <param name="units"></param>
    /// <returns></returns>
    public IEnumerator ShowTargets(List<Unit> units, string targetingDesc)
    {
        Debug.Log("ExecuteManager: potential targets: " + units.Count);

        // chaning hint text
        string savedHintText = GameManager.instance.managerUI.hintMessage.text;
        GameManager.instance.managerUI.NewHint(targetingDesc);

        // highlighting potential targets
        for (int i = 0; i < units.Count; i++)
        {
            units[i].EnableTarget(true);
        }

        // wait until player chooses a target
        while (effectTarget == null && units.Count > 0)
        {
            // immediately choose a target if it's an AI opponent
            if (GameManager.instance.yourTurn == false && GameManager.instance.opponent.isAI)
            {
                Debug.Log("ExecuteManager: AI chose a card");
                effectTarget = units[Random.Range(0, units.Count)];
            }

            yield return null;
        }

        if (effectTarget != null) Debug.Log("ExecuteManager: target chosen: " + effectTarget.card.cardName);

        // returning hint to what it was
        GameManager.instance.managerUI.NewHint(savedHintText);

        // removing target highlight
        for (int i = 0; i < units.Count; i++)
        {
            units[i].EnableTarget(false);
        }
    }

    private void Update()
    {
        // debug testing target show
        if (Input.GetKeyDown(KeyCode.E))
        {
            List<Unit> allUnits = GameManager.instance.fieldManager.GetAllUnits();
            StartCoroutine(ShowTargets(allUnits, "testing targeting"));
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            Unit throwawayUnit = GetTarget();
        }
    }

    public Unit GetTarget(bool removeReferenceToTarget = true)
    {
        Unit unitToReturn = effectTarget;
        if (removeReferenceToTarget) effectTarget = null;
        return unitToReturn;
    }

    public IEnumerator TriggerEffects(string effectType = "onPlay")
    {
        List<IEnumerator> stack = effectStack;

        // choosing the correct effect stack
        switch (effectType)
        {
            case "onRoll":
                stack = rollEffectStack;
                break;
            case "onBattleEnd":
                stack = battleOverEffectStack;
                break;
        }

        // taking effects one by one so effects can safely add new effects to the stack while it is resolving
        while (stack.Count > 0)
        {
            IEnumerator effect = stack[0];
            stack.RemoveAt(0);
            yield return StartCoroutine(effect);
        }
    }
}
