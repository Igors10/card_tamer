using FishNet.Demo.AdditiveScenes;
#if UNITY_EDITOR
using FishNet.Transporting.Tugboat.Editing;
#endif
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class BattleManager : MonoBehaviour // info@entrypoint.lv
{
    [Header("refs")]
    [SerializeField] PowerCounter playerPowerUI;
    [SerializeField] PowerCounter opponentPowerUI;
    public GameObject damageObj;
    [SerializeField] DeathAnim death;

    [Header("prefabs")]
    public GameObject dmgParticlePrefab;
    
    [Header("round end")]
    [SerializeField] GameObject knockedUnitsMessege;
    [SerializeField] float deathThinkingTime;
    [SerializeField] float deathChoosingTime;
    [SerializeField] float afterDeathTime;

    [HideInInspector] public int currentLine;
    [HideInInspector] public bool lostCurrentRound;

    public void ResetBattleVals()
    {
        currentLine = -1;
        lostCurrentRound = false;

        // the old lane power counters aren't used in fights
        playerPowerUI.gameObject.SetActive(false);
        opponentPowerUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by FightManager when a player has no cards left on the field
    /// </summary>
    public void EndBattle()
    {
        StartCoroutine(EndBattleSequence());
    }

    IEnumerator EndBattleSequence()
    {
        // no more turns this round
        GameManager.instance.managerUI.turnHint.gameObject.SetActive(false);

        // the player with cards left deals 1 damage for each of them
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;
        if (player.cardsOnField.Count > 0) yield return DealRoundDamage(player, opponent);
        else if (opponent.cardsOnField.Count > 0) yield return DealRoundDamage(opponent, player);

        yield return WrapUpBattle();
    }

    /// <summary>
    /// A damage particle flies from every card the winner has left to the loser's health
    /// </summary>
    IEnumerator DealRoundDamage(Player winner, Player loser)
    {
        PowerCounter counter = winner.powerCounter;
        Vector3 healthbarPos = loser.playerUI.healthbar.transform.position;

        List<GameObject> particles = new List<GameObject>();
        foreach (Card card in winner.cardsOnField)
        {
            GameObject particle = Instantiate(dmgParticlePrefab, card.transform.position, Quaternion.identity, damageObj.transform);
            particle.GetComponent<UnityEngine.UI.Image>().color = winner.playerColor;
            particles.Add(particle);
            AudioManager.instance.PlaySFX("MoreDamageSFX");
            Animations.instance.PopAnim(particle, 0.3f, 0.45f);
            card.Cheer();
            yield return new WaitForSeconds(0.2f);
        }

        foreach (GameObject particle in particles)
        {
            yield return counter.Damage(healthbarPos, particle);
            loser.TakeDamage(1);
            Destroy(particle);
            if (GameManager.instance.gameOver) yield break;
            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator WrapUpBattle()
    {
        Debug.Log("BattleManager: ending battle phase");

        // stop here if one of the players has lost
        if (GameManager.instance.gameOver) yield break;

        // Message that units are knocked out
        //GameManager.instance.managerUI.hintMessage.gameObject.SetActive(false); // removing it so that the message about doodle collector could be seen
        //knockedUnitsMessege.SetActive(true);

        // DISCARDING STUNNED UNITS
        // ========================
        /*
        // getting all stunned units
        List<Unit> playerStunnedUnits = GameManager.instance.fieldManager.GetStunnedUnits(GameManager.instance.player);
        List<Unit> opponentStunnedUnits = GameManager.instance.fieldManager.GetStunnedUnits(GameManager.instance.opponent);

        // Enalbing death 
        death.gameObject.SetActive(true);
        yield return new WaitForSeconds(deathThinkingTime);

        yield return StartCoroutine(StunnedUnitsDiscard(playerStunnedUnits));
        yield return StartCoroutine(StunnedUnitsDiscard(opponentStunnedUnits));

        yield return new WaitForSeconds(afterDeathTime);
        knockedUnitsMessege.SetActive(false);
        death.gameObject.SetActive(false);
        */
        // ROUND ENDS
        GameManager.instance.managerUI.StateChangeMessage("Round " + GameManager.instance.roundNr + " ends");

        // pause while "round ends" text is on the screen
        while (GameManager.instance.managerUI.stateTransitionObj.activeSelf)
        {
            yield return null;
        }

        lostCurrentRound = GameManager.instance.player.damageTakenThisRound > GameManager.instance.opponent.damageTakenThisRound;

        // CHAT WITH THE RAT
        // Rat encouraging you 
        if (lostCurrentRound) yield return RatSmart.instance.PlayDialogue("lose");
        else yield return RatSmart.instance.PlayDialogue("win");

        // Rat explaining the discard
        if (RatSmart.instance.discardingExplained == false && GameManager.instance.player.cardsInDiscard.Count >= 1)
        {
            yield return RatSmart.instance.PlayDialogue("DiscardDescDial");
            RatSmart.instance.discardingExplained = true;
        }

        // online both discards are rolled now in seat order (host first), so both games get the same numbers and destroy the same cards
        DiscardManager discardManager = GameManager.instance.discardManager;
        int playerPick = -1;
        int opponentPick = -1;
        if (!GameManager.instance.playerConfig.offlineMatch)
        {
            if (GameData.localSeat == 0) { playerPick = discardManager.RollAutoDiscard(GameManager.instance.player); opponentPick = discardManager.RollAutoDiscard(GameManager.instance.opponent); }
            else { opponentPick = discardManager.RollAutoDiscard(GameManager.instance.opponent); playerPick = discardManager.RollAutoDiscard(GameManager.instance.player); }
        }

        // Discarding a card
        GameManager.instance.managerUI.hintMessage.gameObject.SetActive(false);
        if (GameManager.instance.player.cardsInDiscard.Count > 0)
            yield return StartCoroutine(discardManager.StartDiscardSequence(GameManager.instance.player, true, playerPick));
        // AI opponent discards a card
        if (GameManager.instance.opponent.isAI) GameManager.instance.opponent.GetComponent<AIOpponent>().DiscardOpponentUnit();
        // online opponent's discard (rolled above, shown on their own screen)
        else if (opponentPick >= 0) GameManager.instance.opponent.cardsInDiscard[opponentPick].DestroyCard();

        // Ending the phase
        GameManager.instance.player.endStateReady = true;
        GameManager.instance.opponent.endStateReady = true;
        GameManager.instance.EndTurn();
    }

}
