using FishNet.Component.Transforming.Beta;
using FishNet.Utility.Extension;
using System;
using System.Collections;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    Player player;

    [Header("HP")]
    public GameObject healthbar;
    [SerializeField] Image healthbarFill;
    [SerializeField] TextMeshProUGUI hpValue;

    [Header("Damage effects")]
    [SerializeField] ParticleSystem damageVFX;
    [SerializeField] float shakeLenght;
    [SerializeField] float shakeIntensity; 

    [Header("Avatar")]
    [SerializeField] Image head;
    [SerializeField] Image face;
    [SerializeField] Color avatarColor;

    [Header("Food tokens")]
    public FoodToken[] foodCounters = new FoodToken[3];
    [SerializeField] GameObject foodObj;
    [SerializeField] float offsetX;
    [SerializeField] float tokenStayTime;
    Vector3 defaultTokenPos;
    Vector3 shownTokenPos;


    private void Start()
    {
        // getting reference for the player
        player = GetComponent<Player>();

        // Updating the hp bar visuals
        RefreshHP();

        // Getting positions for food tokens UI positions
        defaultTokenPos = foodObj.transform.localPosition;
        shownTokenPos = (player.isOpponent) ? defaultTokenPos - new Vector3(offsetX, 0, 0) : defaultTokenPos + new Vector3(offsetX, 0, 0);
    }

    /// <summary>
    /// Updates hp value on the healthbar (and plays damage animation if needed)
    /// </summary>
    /// <param name="isDamage"></param>
    public void RefreshHP(int damage = 0)
    {
        // Updating the text
        hpValue.text = player.health.ToString();

        // Updating the healthbar fill
        if (healthbarFill != null)
        {
            float newHealthbarFill = (float)player.health / (float)GameManager.instance.startingMaxHealth;
            healthbarFill.fillAmount = newHealthbarFill;
        }

        if (damage > 0) StartCoroutine(DamageEffect(damage));
    } 

    /// <summary>
    /// Juice for taking damage
    /// </summary>
    /// <param name="intensity"></param>
    /// <returns></returns>
    IEnumerator DamageEffect(float damage)
    {
        // playing the particle effect
        if (damageVFX != null) damageVFX.Play();

        // shaking the healthbar for extra juice
        float t = 0;
        float maxIntensity = shakeIntensity * damage;
        Vector3 startingPosition = healthbar.transform.localPosition;

        while (t < shakeLenght)
        {
            // Gradually decreasing the intensity
            t += Time.deltaTime;
            float actualT = t / shakeLenght;
            float currentIntensity = Mathf.Lerp(maxIntensity, 0f, actualT);

            // Random position offset
            float xOffset = UnityEngine.Random.Range(-1, 1) * currentIntensity;
            float yOffset = UnityEngine.Random.Range(-1, 1) * currentIntensity;

            healthbar.transform.localPosition += new Vector3(xOffset, yOffset, 0);

            yield return null;

            // Reverting the offset
            healthbar.transform.localPosition = startingPosition;
        }
    }

    /// <summary>
    /// Add (or reduce) food tokens on the player
    /// </summary>
    /// <param name="type"></param>
    /// <param name="amount"></param>
    public void AddRandomFoodToken(bool showing = false)
    {
        FoodType type = (FoodType)UnityEngine.Random.Range(0, Enum.GetNames(typeof(FoodType)).Length);
        player.food[(int)type]++;
        StartCoroutine(TokenUpdateAnim(type, showing));
    }
    public IEnumerator ShowTokens(bool show)
    {
        Vector3 startingPosition = foodObj.transform.localPosition;
        Vector3 targetPosition = (show) ? shownTokenPos : defaultTokenPos;
        float t = 0;
        float timeAppearing = 0.6f;

        while (t < timeAppearing)
        {
            t += Time.deltaTime;
            float clampedT = t / timeAppearing;
            float coolT = (show) ? 1 - (1 - clampedT) * (1 - clampedT) : clampedT * clampedT; 

            foodObj.transform.localPosition = Vector3.Lerp(startingPosition, targetPosition, coolT);

            yield return null;
        }

        // snapping to correct position
        foodObj.transform.localPosition = targetPosition;
    }
    IEnumerator TokenUpdateAnim(FoodType type, bool showing)
    {
        if (showing) yield return StartCoroutine(ShowTokens(true));
        foodCounters[(int)type].RefreshToken(true);

        yield return new WaitForSeconds(tokenStayTime);
        if (showing) yield return StartCoroutine(ShowTokens(false));
    }
}
