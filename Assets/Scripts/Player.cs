using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [Header("player attributes")]
    public int health;
    public List<Card> cardsInHand = new List<Card>();
    public List<Card> cardsOnField = new List<Card>();
    public Field[] fields = new Field[4];
    AIOpponent AIplayer;
    public bool isAI;

    [Header("resources")]
    [HideInInspector] public int berries = 1;
    [HideInInspector] public int meat = 1;
    [HideInInspector] public int fish = 1;

    private void Start()
    {
        InitPlayer();
    }

    void InitPlayer()
    {
        // checking if its AI
        AIplayer = GetComponent<AIOpponent>();
        if (AIplayer != null ) isAI = true;

        // heatlh
        health = GameManager.instance.startingMaxHealth;

        // fields
        if (this == GameManager.instance.player)
        GameManager.instance.fieldManager.fields = fields;
    }

    public void StartTurn()
    {
        if (isAI) AIplayer.AIStartTurn();
    }
}
