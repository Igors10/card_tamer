using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    
    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("Healthbar")]
    int damageToHP; // how much hp is currently missing
    [SerializeField]Image[] hearts = new Image[10];
    [SerializeField] TextMeshProUGUI healthText;

    [Header("refs")]
    public Ability[] abilities = new Ability[2];
    [SerializeField] RectTransform rt;
    [SerializeField] Image cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
   

    /// <summary>
    ///  makes all the card data and visuals match the creature data and current state
    /// </summary>
    public void Refresh()
    {
        // NAME
        nameText.text = cardData.name;

        // SPRITE
        cardSprite.sprite = cardData.creatureSprite;

        // HEALTH (number)
        int currentHP = cardData.health - damageToHP;
        if (currentHP < 0) currentHP = 0;

        healthText.text = currentHP.ToString();

        // HEALTH (heart visuals)
        for (int a = hearts.Length - 1; a >= 0; a--)
        {
            // Showing amount of hearts corresponding to max hp value
            hearts[a].gameObject.SetActive(a < cardData.health);

            // Coloring hearts black according to damage taken
            Color fullHeartColor = new Color(1f, 1f, 1f, 1f);
            Color emptyHeartColor = new Color(0f, 0f, 0f, 1f);
            Color heartColor = (cardData.health - damageToHP < a) ? fullHeartColor : emptyHeartColor;
        }

        // ABILITIES

    }

    /// <summary>
    /// Rotates the card clockwise
    /// </summary>
    /// <param name="rotationAngle"></param>
    public void RotateCard(float rotationAngle)
    {
        transform.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
    }

    /// <summary>
    /// Assign abilities from cardData
    /// </summary>
    public void AssignAbilies()
    {
        for (int i = 0; i < abilities.Length; i++) { abilities[i].InitAbility(cardData.ability[i]); }
    }

    public void AssignCardData(CreatureObj newCardData)
    {
        cardData = newCardData;
        AssignAbilies();
        Refresh();
    }
}
