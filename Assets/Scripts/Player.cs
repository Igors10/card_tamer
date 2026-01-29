using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("player attributes")]
    public int health;
    List<Card> startingHand = new List<Card>();
    public List<Card> cardsInHand = new List<Card>();
    public List<Card> cardsOnField = new List<Card>();
    public Field[] fields = new Field[4];
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
        // heatlh
        health = GameManager.instance.startingMaxHealth;

        // fields
        GameManager.instance.fieldManager.fields = fields;
    }
}
