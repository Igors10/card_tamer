using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerConfigData", menuName = "PlayerConfig")]
public class PlayerConfigObj : ScriptableObject
{
    public List<CreatureObj> startingCards = new List<CreatureObj>();
    public Color playerColor = Color.white;
    public bool offlineMatch = true;

    public void ResetCardConfig()
    {
        startingCards.Clear();
    }
}
