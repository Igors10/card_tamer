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
    [SerializeField] float beforeHeartBreakTime = 0.5f; // winner's cards cheer first
    [SerializeField] float afterHeartBreakTime = 1f; // the broken heart is seen before the round (or the game) ends

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

        // phantoms disappear before damage is counted
        FightManager.instance.RemoveAllPhantoms();

        // the player with no cards left loses one heart (however many cards the winner has left)
        Player player = GameManager.instance.player;
        Player opponent = GameManager.instance.opponent;
        if (player.cardsOnField.Count > 0 && opponent.cardsOnField.Count == 0) yield return BreakHeart(player, opponent);
        else if (opponent.cardsOnField.Count > 0 && player.cardsOnField.Count == 0) yield return BreakHeart(opponent, player);

        yield return WrapUpBattle();
    }

    /// <summary>
    /// Winner's cards cheer, then one of the loser's hearts breaks. No hearts left: the loser loses the game
    /// </summary>
    IEnumerator BreakHeart(Player winner, Player loser)
    {
        foreach (Card card in winner.cardsOnField) card.Cheer();
        yield return new WaitForSeconds(beforeHeartBreakTime);

        loser.LoseHeart();
        yield return new WaitForSeconds(afterHeartBreakTime);

        if (loser.health < 1) loser.PlayerDeath();
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

        // both discards are rolled now in seat order (host first, offline the player is seat 0), so online both games destroy the same cards
        DiscardManager discardManager = GameManager.instance.discardManager;
        int playerPick, opponentPick;
        if (GameData.localSeat == 0 || GameManager.instance.playerConfig.offlineMatch) { playerPick = discardManager.RollAutoDiscard(GameManager.instance.player); opponentPick = discardManager.RollAutoDiscard(GameManager.instance.opponent); }
        else { opponentPick = discardManager.RollAutoDiscard(GameManager.instance.opponent); playerPick = discardManager.RollAutoDiscard(GameManager.instance.player); }

        // Scissor Rat destroys one knocked out card from each side (both rows shown together)
        GameManager.instance.managerUI.hintMessage.gameObject.SetActive(false);
        yield return StartCoroutine(discardManager.StartRoundDiscard(playerPick, opponentPick));

        // Ending the phase
        GameManager.instance.player.endStateReady = true;
        GameManager.instance.opponent.endStateReady = true;
        GameManager.instance.EndTurn();
    }

}
