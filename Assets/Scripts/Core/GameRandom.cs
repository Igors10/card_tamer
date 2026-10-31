using UnityEngine;

/// <summary>
/// Random numbers for gameplay (dice, random targets, random lanes...).
/// Online, both players seed it with the same number, so both games roll the same results.
/// Visual randomness (particles, shakes, tilts) must keep using UnityEngine.Random, otherwise the two games drift apart.
/// Only use it in code that runs the same way on both machines.
/// </summary>
public static class GameRandom
{
    static System.Random random = new System.Random();

    public static int Seed { get; private set; }
    public static int CallCount { get; private set; } // how many numbers were rolled since Init, handy for spotting desyncs

    /// <summary>
    /// Starts a new sequence of numbers. Same seed = same numbers.
    /// </summary>
    public static void Init(int seed)
    {
        Seed = seed;
        CallCount = 0;
        random = new System.Random(seed);

        Debug.Log("GameRandom: seeded with " + seed);
    }

    /// <summary>
    /// Makes a new random seed (for offline matches, or for the host to send out)
    /// </summary>
    public static int NewSeed()
    {
        return UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    /// <summary>
    /// Random int from min (included) to max (excluded), same as UnityEngine.Random.Range for ints
    /// </summary>
    public static int Range(int min, int max)
    {
        CallCount++;
        return random.Next(min, max);
    }

    /// <summary>
    /// Random float from 0 to 1, same as UnityEngine.Random.value
    /// </summary>
    public static float Value()
    {
        CallCount++;
        return (float)random.NextDouble();
    }
}
