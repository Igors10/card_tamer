using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIConfig", menuName = "AIConfig")]
public class AIConfigObj : ScriptableObject
{
    public List<CreatureObj> startingCardOptions = new List<CreatureObj>();
    public List<CreatureObj> startingSpecialOptions = new List<CreatureObj>();

    // turn waiting times (delays)
    public float placingDelay;
    public float executingRevealDelay;
    public float executingAbilityDelay;
    public float battlingDelay;
    public float buyingDelay;
}
