using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [Header("refs")]
    public PlayerUI playerUI;
    public List<Card> cardsInHand = new List<Card>();
    public List<Card> cardsOnField = new List<Card>();
    public List<Card> plannedCardStack = new List<Card>();
    public Field[] fields = new Field[4];
    [HideInInspector] public PowerCounter powerCounter;

    [Header("player attributes")]
    public int health;
    public bool endStateReady = false;
    

    [Header("config")]
    AIOpponent AIplayer;
    public bool isAI;
    public Color playerColor;

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
    }

    public void StartTurn()
    {
        if (isAI) AIplayer.AIStartTurn();
    }

    public void TakeDamage(int damageAmount)
    {
        damageAmount = (damageAmount > health) ? health : damageAmount;

        health -= damageAmount;
        playerUI.RefreshHP(damageAmount);
    }
}
