using System.Collections.Generic;
using System.Text;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public enum CommandType : byte
{
    Ready,          // ready button ("Finish placing", workshop "Ready")
    PickColor,      // workshop pencil color
    CreateCard,     // workshop made a card
    DiscardFromHand, // workshop: card destroyed for a coin
    PlayCard,       // placing: card from hand to a lane
    ChooseTarget,   // ability target
    StopDice        // clicked to stop rolling dice
}

/// <summary>
/// One player decision sent over the network. FishNet sends structs with public fields automatically
/// </summary>
public struct MatchCommand
{
    public int number;      // order given by the server, 0, 1, 2...
    public int seat;        // who made it (set by the server)
    public int stateNr;     // GameManager.stateNr when it was made
    public CommandType type;

    // values, which ones are used depends on the type
    public int index;           // DiscardFromHand, PlayCard: card in hand. CreateCard: unit preset (CardDatabase.GetAllPresets), -1 = drawing
                                // ChooseTarget: unit in ExecuteManager.targetOptions. StopDice: how many dice
    public int lane;            // PlayCard: field in Player.fields
    public string cardName;     // CreateCard
    public string abilityName;  // CreateCard: base sticker (AbilityObj asset name)
    public bool onRight;        // CreateCard: base sticker side
    public Color color;         // PickColor: player color. CreateCard: second color
    public Color abilityColor;  // CreateCard
    public byte[] layer1;       // CreateCard: drawing layers as PNG
    public byte[] layer2;
}

/// <summary>
/// Networked object in the Board scene (does nothing in bot matches).
/// Starts the match: waits until both players have loaded the Board, then the server picks the random seed and the starting player.
/// Commands: clients send their decisions to the server, the server numbers them and sends them to both players,
/// both games apply them in the same order (the sender too, nothing is applied before the server sends it back).
/// </summary>
public class MatchNetwork : NetworkBehaviour
{
    public static MatchNetwork instance;

    // server only
    readonly Dictionary<int, int> seatByClient = new Dictionary<int, int>(); // ClientId -> seat, for players that have loaded the Board
    bool matchStarted;
    int nextCommandNumber;
    readonly Dictionary<int, string> firstSnapshots = new Dictionary<int, string>(); // stateNr -> snapshot of whoever got there first

    // client
    readonly Queue<MatchCommand> commandQueue = new Queue<MatchCommand>();
    int expectedCommandNumber;

    void Awake()
    {
        instance = this;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // this player's Board is loaded and the object is spawned, telling the server
        ClientLoaded(GameData.localSeat);
    }

    [ServerRpc(RequireOwnership = false)]
    void ClientLoaded(int seat, NetworkConnection sender = null)
    {
        seatByClient[sender.ClientId] = seat;
        Debug.Log("MatchNetwork: client " + sender.ClientId + " (seat " + seat + ") loaded the Board (" + seatByClient.Count + "/" + ConnectionScript.maxPlayers + ")");

        if (matchStarted || seatByClient.Count < ConnectionScript.maxPlayers) return;
        matchStarted = true;

        int seed = GameRandom.NewSeed();
        int firstSeat = Random.Range(0, ConnectionScript.maxPlayers);
        StartMatchRpc(seed, firstSeat);
    }

    [ObserversRpc]
    void StartMatchRpc(int seed, int firstSeat)
    {
        GameManager.instance.StartOnlineMatch(seed, firstSeat);
    }

    // ==================== COMMANDS ============================

    public void SendReady()
    {
        SendCommand(new MatchCommand { type = CommandType.Ready });
    }

    public void SendPlayCard(int handIndex, int lane)
    {
        SendCommand(new MatchCommand { type = CommandType.PlayCard, index = handIndex, lane = lane });
    }

    public void SendChooseTarget(int optionIndex)
    {
        SendCommand(new MatchCommand { type = CommandType.ChooseTarget, index = optionIndex });
    }

    /// <summary>
    /// The sender's dice already stopped, the other game keeps them rolling until this comes
    /// </summary>
    public void SendStopDice(int count)
    {
        SendCommand(new MatchCommand { type = CommandType.StopDice, index = count });
    }

    // Workshop results: the sender's game applies them right away (the workshop only changes your own hand),
    // the commands are only applied by the other game

    public void SendPickColor(Color color)
    {
        SendCommand(new MatchCommand { type = CommandType.PickColor, color = color });
    }

    /// <summary>
    /// preset = the unit preset used when the canvas was empty, otherwise layers = the drawing
    /// </summary>
    public void SendCreateCard(CreatureObj cardData, UnitPreset preset, Texture2D[] layers)
    {
        MatchCommand command = new MatchCommand
        {
            type = CommandType.CreateCard,
            cardName = cardData.name,
            abilityName = cardData.BaseSticker.name,
            onRight = cardData.stickers[0].onRight,
            color = cardData.secondaryColor,
            abilityColor = cardData.abilityColor,
            index = -1
        };

        if (preset != null) command.index = GameManager.instance.cardDatabase.GetAllPresets().IndexOf(preset);
        else
        {
            command.layer1 = layers[0].EncodeToPNG();
            command.layer2 = layers[1].EncodeToPNG();
        }

        SendCommand(command);
    }

    public void SendDiscardFromHand(int handIndex)
    {
        SendCommand(new MatchCommand { type = CommandType.DiscardFromHand, index = handIndex });
    }

    void SendCommand(MatchCommand command)
    {
        command.stateNr = GameManager.instance.stateNr;
        Debug.Log("MatchNetwork: sending " + command.type);
        CommandServer(command);
    }

    [ServerRpc(RequireOwnership = false)]
    void CommandServer(MatchCommand command, NetworkConnection sender = null)
    {
        if (!seatByClient.TryGetValue(sender.ClientId, out int seat)) return;

        command.seat = seat;
        command.number = nextCommandNumber++;
        CommandRpc(command);
    }

    [ObserversRpc]
    void CommandRpc(MatchCommand command)
    {
        commandQueue.Enqueue(command);
    }

    void Update()
    {
        // applying commands in order, each one waits until this game is at the point where it was made
        while (commandQueue.Count > 0 && CanApply(commandQueue.Peek()))
        {
            Apply(commandQueue.Dequeue());
        }
    }

    bool CanApply(MatchCommand command)
    {
        GameManager gm = GameManager.instance;

        // the other player is already in the next state (skipped animations), waiting until we get there too
        if (command.stateNr > gm.stateNr) return false;

        // targets wait until this game is asking for one too (the other game might still be animating)
        if (command.type == CommandType.ChooseTarget) return gm.executeManager.targetOptions != null;

        // own commands were made on our turn, so they fit right away
        if (command.seat == GameData.localSeat) return true;

        // the other player's dice have to be rolling here too
        if (command.type == CommandType.StopDice) return D6.CountRolling(gm.opponent) >= command.index;

        // the other player's moves wait until it's their turn here too (in the workshop both act at the same time)
        return gm.currentState == GameState.BUYING || gm.waitingForRemote;
    }

    void Apply(MatchCommand command)
    {
        GameManager gm = GameManager.instance;

        if (command.number != expectedCommandNumber) Debug.LogError("MatchNetwork: expected command " + expectedCommandNumber + ", got " + command.number);
        if (command.stateNr < gm.stateNr) Debug.LogError("MatchNetwork: command " + command.number + " is from an earlier state (" + command.stateNr + ", now " + gm.stateNr + "), games are out of sync");
        expectedCommandNumber = command.number + 1;

        Player p = (command.seat == GameData.localSeat) ? gm.player : gm.opponent;
        if (p.isRemote) gm.waitingForRemote = false;

        Debug.Log("MatchNetwork: applying command " + command.number + " " + command.type + " from [" + p.playerName + "]");

        switch (command.type)
        {
            case CommandType.Ready:
                gm.PlayerReady(p);
                break;

            case CommandType.PlayCard:
                ApplyPlayCard(command, p);
                break;

            case CommandType.ChooseTarget:
                ApplyChooseTarget(command);
                break;

            // the sender's game already did these
            case CommandType.StopDice:
                if (p.isRemote) D6.StopDice(p, command.index);
                break;

            case CommandType.PickColor:
                if (p.isRemote) ApplyPickColor(command, p);
                break;

            case CommandType.CreateCard:
                if (p.isRemote) ApplyCreateCard(command, p);
                break;

            case CommandType.DiscardFromHand:
                if (p.isRemote) ApplyDiscardFromHand(command, p);
                break;
        }
    }

    void ApplyPlayCard(MatchCommand command, Player p)
    {
        if (command.index < 0 || command.index >= p.cardsInHand.Count) { Debug.LogError("MatchNetwork: no card " + command.index + " in [" + p.playerName + "]'s hand"); return; }
        if (command.lane < 0 || command.lane >= p.fields.Length) { Debug.LogError("MatchNetwork: no lane " + command.lane); return; }

        p.fields[command.lane].PlayCardNow(p.cardsInHand[command.index], p);
    }

    void ApplyChooseTarget(MatchCommand command)
    {
        ExecuteManager executeManager = GameManager.instance.executeManager;
        if (command.index < 0 || command.index >= executeManager.targetOptions.Count) { Debug.LogError("MatchNetwork: no target option " + command.index); return; }

        executeManager.effectTarget = executeManager.targetOptions[command.index];
    }

    void ApplyPickColor(MatchCommand command, Player p)
    {
        p.playerColor = command.color;
        p.powerCounter.RefreshColor();
    }

    void ApplyCreateCard(MatchCommand command, Player p)
    {
        GameManager gm = GameManager.instance;

        AbilityObj ability = gm.cardGenerator.GetAbilityByName(command.abilityName);
        if (ability == null) Debug.LogError("MatchNetwork: no ability called " + command.abilityName);

        Sprite[] sprites = (command.index >= 0)
            ? gm.cardDatabase.GetAllPresets()[command.index].sprite
            : new Sprite[] { PngToSprite(command.layer1), PngToSprite(command.layer2) };

        CreatureObj cardData = gm.cardGenerator.ConstructNewCard(command.cardName, sprites, new Sticker(ability, command.onRight), command.color, command.abilityColor);
        gm.cardGenerator.CreateCard(cardData, p);
    }

    void ApplyDiscardFromHand(MatchCommand command, Player p)
    {
        if (command.index < 0 || command.index >= p.cardsInHand.Count) { Debug.LogError("MatchNetwork: no card " + command.index + " in [" + p.playerName + "]'s hand"); return; }

        p.cardsInHand[command.index].DestroyCard();
        GameManager.instance.handManager.UpdateHandVisuals(p);
    }

    // ==================== DESYNC CHECK ============================
    // At every state change both games should be exactly the same, so both send a text snapshot
    // and the server compares them. Different = some gameplay code ran differently on the two games

    /// <summary>
    /// Called by GameManager.TransitionGameState (online), before the new state does anything
    /// </summary>
    public void CheckSync()
    {
        SnapshotServer(GameManager.instance.stateNr, GetSnapshot());
    }

    [ServerRpc(RequireOwnership = false)]
    void SnapshotServer(int stateNr, string snapshot)
    {
        // first one waits for the other
        if (!firstSnapshots.TryGetValue(stateNr, out string otherSnapshot)) { firstSnapshots[stateNr] = snapshot; return; }
        firstSnapshots.Remove(stateNr);

        if (snapshot != otherSnapshot) DesyncRpc(stateNr, otherSnapshot, snapshot);
    }

    [ObserversRpc]
    void DesyncRpc(int stateNr, string snapshotA, string snapshotB)
    {
        Debug.LogError("MatchNetwork: games are OUT OF SYNC at state " + stateNr + "\n---- game A ----\n" + snapshotA + "\n---- game B ----\n" + snapshotB);
    }

    /// <summary>
    /// Everything gameplay related, seat 0 first so it reads the same on both games
    /// </summary>
    string GetSnapshot()
    {
        GameManager gm = GameManager.instance;
        Player seat0 = (GameData.localSeat == 0) ? gm.player : gm.opponent;
        Player seat1 = gm.GetOpponentOfPlayer(seat0);

        StringBuilder snapshot = new StringBuilder();
        snapshot.Append(gm.currentState).Append(", round ").Append(gm.roundNr).Append(", random numbers used ").Append(GameRandom.CallCount);
        AppendPlayer(snapshot, "seat 0", seat0);
        AppendPlayer(snapshot, "seat 1", seat1);
        return snapshot.ToString();
    }

    static void AppendPlayer(StringBuilder snapshot, string label, Player p)
    {
        snapshot.Append('\n').Append(label).Append(": hp ").Append(p.health).Append(", stars ").Append(p.maxStars)
            .Append(", random lane ").Append(p.playsOnRandomLane).Append(", must play special ").Append(p.mustPlaySpecial);

        snapshot.Append("\n  hand:");
        foreach (Card card in p.cardsInHand) snapshot.Append(" [").Append(card.cardData.name).Append(']');

        snapshot.Append("\n  knocked out:");
        foreach (Card card in p.cardsInDiscard) snapshot.Append(" [").Append(card.cardData.name).Append(']');

        for (int i = 0; i < p.fields.Length; i++)
        {
            Field field = p.fields[i];
            snapshot.Append("\n  lane ").Append(i).Append(" (blocked ").Append(field.roundsBlocked).Append("):");
            foreach (Unit unit in field.units)
            {
                if (unit == null) snapshot.Append(" [-]");
                else snapshot.Append(" [").Append(unit.card.cardData.name).Append(' ').Append(unit.card.currentPower).Append(']');
            }
        }
    }

    static Sprite PngToSprite(byte[] png)
    {
        if (png == null || png.Length == 0) return null;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(png);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
