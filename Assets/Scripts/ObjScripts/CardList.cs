using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Card list", menuName = "CardList")]
// This is a scriptable object that holds all the creature objs (cards)
public class CardList : ScriptableObject 
{
    public List<CreatureObj> cardList = new List<CreatureObj>();  
}
