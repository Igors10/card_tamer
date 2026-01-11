using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Card : MonoBehaviour
{
    [Header("CreatureData")]
    public CreatureObj cardData;
    // delete
    [SerializeField] float rotation_z;
    [SerializeField] float hearts_z;

    [Header("Prefabs")]
    public GameObject activeAbility;
    [SerializeField] GameObject heart;

    [Header("Healthbar")]
    int damageToHP; // how much hp is currently missing
    Image[] hearts = new Image[10];
    [SerializeField] TextMeshProUGUI healthText;
    [SerializeField] Transform healthField;
    [SerializeField] float heartSpacing;
    [SerializeField] RectTransform heartsObject;

    [Header("refs")]
    Ability[] abilities = new Ability[2];
    [SerializeField] RectTransform rt;
    [SerializeField] Image cardSprite;
    [SerializeField] TextMeshProUGUI nameText;
   

    private void Start()
    {
        initHP();
    }

    /// <summary>
    /// Creates the hearts in the hp field and puts them in the 'hearts' array
    /// </summary>
    void initHP()
    {
        for (int a = 1; a < hearts.Length + 1; a++)
        {
            float newHeartY = healthText.rectTransform.position.y;
            float newHeartX = healthText.rectTransform.position.x;
            newHeartX += (a % 2 == 0) ? heartSpacing * a : heartSpacing * -a - 10;
            Vector2 newHeartPosition = new Vector2(newHeartX, newHeartY);
            GameObject newHeart = Instantiate(heart, newHeartPosition, Quaternion.identity, heartsObject.transform);
            hearts[a - 1] = newHeart.GetComponent<Image>();
            Debug.Log("Card: new hearts local position is: " + newHeart.transform.localPosition);
        }
    }

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
       
        for (int a = hearts.Length - 1; a > -1; a--)
        {
            // Showing amount of hearts corresponding to max hp value
            //hearts[a].gameObject.SetActive(a <= cardData.health);

            // Coloring hearts black according to damage taken
            //Color heartColor = (cardData.health - damageToHP < a) ? new Color(1f, 1f, 1f, 1f) : new Color(0f,0f,0f,1f);
        }

        // ABILITIES

    }

    public void AssignCardData(CreatureObj newCardData)
    {
        cardData = newCardData;
        Refresh();
    }

    private void Update()
    {
        heartsObject.localRotation = Quaternion.Euler(0f, 0f, rt.localRotation.eulerAngles.z);
        rotation_z = rt.localRotation.eulerAngles.z;
        hearts_z = heartsObject.transform.localRotation.eulerAngles.z;
    }
}
