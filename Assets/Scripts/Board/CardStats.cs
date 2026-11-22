using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What a card did during the whole game, shown on the game over screen. Each game counts it locally
/// (online both games run the same fights, so nothing is sent)
/// </summary>
public class CardStats
{
    // fights
    public int knockouts; // units it knocked out in fights
    public int timesKnockedOut;
    public int highestPower;
    public int fightsWon;
    public int winStreak;
    public int bestWinStreak;
    public int sixes; // base power dice that rolled a 6
    public int roundsSurvived; // still on the field when the round ended

    // stickers
    public int guards; // Guard took an attack for its neighbour
    public int reveals; // Scout
    public int locks; // Lock
    public int rightGuesses; // Seer
    public int mixUps; // Mixer
    public int sneaks; // Sneaky
    public int cheers; // Cheerer
    public int supports; // Support

    // specials
    public int destroyed; // Beast
    public int confused; // Joker swapped the attacker
    public int phantomWins; // fights its Phantom copy won
    public int dragonShots; // Dragon
    public int banditHides; // Bandit turned cards face down

    public void RecordPower(int power) => highestPower = Mathf.Max(highestPower, power);

    public void RecordFight(bool won)
    {
        winStreak = won ? winStreak + 1 : 0;
        bestWinStreak = Mathf.Max(bestWinStreak, winStreak);
        if (won) fightsWon++;
    }
}

/// <summary>
/// Picks the line shown under each card on the game over screen: its most impressive stat, or a fluff text
/// </summary>
public static class CardStatTexts
{
    /// <summary>
    /// minValue: below it the stat isn't impressive. impressiveValue: the stat's score is value / impressiveValue, the highest score wins.
    /// exclusive: only the card that's best at it among your cards can show it (sticker stats can show on every card that has them)
    /// </summary>
    class StatLine
    {
        public System.Func<CardStats, int> value;
        public int minValue;
        public float impressiveValue;
        public bool exclusive;
        public string text; // {name}, {x}, {s} = "s" unless x is 1

        public StatLine(System.Func<CardStats, int> value, int minValue, float impressiveValue, bool exclusive, string text)
        {
            this.value = value;
            this.minValue = minValue;
            this.impressiveValue = impressiveValue;
            this.exclusive = exclusive;
            this.text = text;
        }
    }

    // every text starts with "{name} ", so the name can be left out
    static readonly StatLine[] statLines =
    {
        new StatLine(s => s.knockouts, 2, 3f, true, "{name} knocked out {x} unit{s}"),
        new StatLine(s => s.timesKnockedOut, 3, 4f, true, "{name} was knocked out {x} times"),
        new StatLine(s => s.highestPower, 8, 10f, true, "{name} had the highest power of {x}"),
        new StatLine(s => s.fightsWon, 2, 3f, true, "{name} won {x} battles"),
        new StatLine(s => s.bestWinStreak, 3, 3f, true, "{name} won {x} battles in a row"),
        new StatLine(s => s.sixes, 2, 2f, true, "{name} rolled a \"6\" {x} times"),
        new StatLine(s => s.roundsSurvived, 2, 3f, true, "{name} stayed alive for {x} rounds"),

        new StatLine(s => s.guards, 1, 1.5f, false, "{name} protected a card {x} time{s}"),
        new StatLine(s => s.reveals, 2, 2.5f, false, "{name} revealed {x} cards"),
        new StatLine(s => s.locks, 2, 2.5f, false, "{name} locked {x} cards"),
        new StatLine(s => s.rightGuesses, 1, 1.5f, false, "{name} guessed {x} hidden card{s} right"),
        new StatLine(s => s.mixUps, 1, 1.5f, false, "{name} mixed up the cards {x} time{s}"),
        new StatLine(s => s.sneaks, 2, 2.5f, false, "{name} sneaked away {x} times"),
        new StatLine(s => s.cheers, 2, 2.5f, false, "{name} cheered for its friends {x} times"),
        new StatLine(s => s.supports, 2, 2.5f, false, "{name} supported a friend {x} times"),

        new StatLine(s => s.destroyed, 1, 1f, false, "{name} has brutally destroyed {x} card{s}"),
        new StatLine(s => s.confused, 1, 1.2f, false, "{name} has confused {x} opponent's card{s}"),
        new StatLine(s => s.phantomWins, 1, 1.2f, false, "{name} had a phantom that won {x} battle{s}"),
        new StatLine(s => s.dragonShots, 1, 1.5f, false, "{name} has shot {x} card{s}"),
        new StatLine(s => s.banditHides, 3, 4f, false, "{name} has hidden {x} cards"),
    };

    // {enemy} = a random opponent card, {mood} = happy you won / sad you lost
    static readonly string[] fluffLines =
    {
        "{name} is ugly",
        "{name} rocks!",
        "{name} hates {enemy}",
        "{name} will try better next time",
        "{name} is ridiculous",
        "{name} is very pretty",
        "{name} is {mood}",
    };

    /// <summary>
    /// One line for each card (same order). won: the local player won the game
    /// </summary>
    public static string[] Pick(List<Card> cards, List<Card> enemyCards, bool won, bool hideName)
    {
        string[] lines = new string[cards.Count];

        // every impressive stat of every card, the most impressive first
        List<(int card, StatLine stat, float score)> candidates = new List<(int, StatLine, float)>();
        for (int i = 0; i < cards.Count; i++)
        {
            foreach (StatLine stat in statLines)
            {
                int value = stat.value(cards[i].stats);
                // a bit of randomness, so equally good stats take turns
                if (value >= stat.minValue) candidates.Add((i, stat, value / stat.impressiveValue + Random.Range(0f, 0.01f)));
            }
        }
        candidates.Sort((a, b) => b.score.CompareTo(a.score));

        // each card gets its best stat, an exclusive stat goes to the card that's best at it
        HashSet<StatLine> usedStats = new HashSet<StatLine>();
        foreach ((int card, StatLine stat, float score) in candidates)
        {
            if (lines[card] != null || (stat.exclusive && usedStats.Contains(stat))) continue;
            usedStats.Add(stat);
            int value = stat.value(cards[card].stats);
            lines[card] = stat.text.Replace("{x}", value.ToString()).Replace("{s}", value == 1 ? "" : "s");
        }

        // the rest get fluff, a different one each while there are enough
        List<string> fluff = new List<string>();
        for (int i = 0; i < cards.Count; i++)
        {
            if (lines[i] != null) continue;
            if (fluff.Count == 0)
            {
                fluff.AddRange(fluffLines);
                if (enemyCards.Count == 0) fluff.Remove("{name} hates {enemy}");
            }
            string line = fluff[Random.Range(0, fluff.Count)];
            fluff.Remove(line);
            if (enemyCards.Count > 0) line = line.Replace("{enemy}", enemyCards[Random.Range(0, enemyCards.Count)].cardName);
            lines[i] = line.Replace("{mood}", won ? "happy you won" : "sad you lost");
        }

        for (int i = 0; i < lines.Length; i++) lines[i] = FillName(lines[i], cards[i].cardName, hideName);
        return lines;
    }

    /// <summary>
    /// Puts the card name in, or leaves it out ("{name} rocks!" -> "Rocks!")
    /// </summary>
    static string FillName(string line, string cardName, bool hideName)
    {
        if (!hideName) return line.Replace("{name}", cardName);
        line = line.Replace("{name} ", "");
        return char.ToUpper(line[0]) + line.Substring(1);
    }
}
