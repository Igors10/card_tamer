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
    [HideInInspector] public bool dead;
    

    [Header("config")]
    AIOpponent AIplayer;
    public bool isAI;
    public Color playerColor;
    [HideInInspector] public bool isOpponent;

    [Header("resources")]
    [HideInInspector] public int[] food = new int[3];


    private void Start()
    {
        InitPlayer();
    }

    void InitPlayer()
    {
        // checking if its AI
        AIplayer = GetComponent<AIOpponent>();
        if (AIplayer != null ) isAI = true;
        if (GameManager.instance.opponent == this) isOpponent = true;

        // heatlh
        health = GameManager.instance.startingMaxHealth;

        // food
        for (int i = 0; i < food.Length; i++)
        {
            food[i] = GameManager.instance.startingResourceAmount;
        }
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

        if (health < 1) PlayerDeath();
    }
    void PlayerDeath()
    {
        dead = true;
        GameManager.instance.GameOver(this);
    }
}
