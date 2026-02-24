using System.Collections;
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
    [SerializeField] FoodToken[] foodCounters = new FoodToken[3];
    [SerializeField] GameObject foodObj;
    [SerializeField] float offsetY;
    [SerializeField] float tokenStayTime;


    private void Start()
    {
        player = GetComponent<Player>();
        RefreshHP();
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
            float xOffset = Random.Range(-1, 1) * currentIntensity;
            float yOffset = Random.Range(-1, 1) * currentIntensity;

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
    public void AddFoodToken(FoodType type, int amount)
    {
        player.food[(int)type] += amount;
        StartCoroutine(TokenUpdateAnim(type));
    }
    IEnumerator ShowTokens(bool show)
    {
        Vector3 startingPosition = foodObj.transform.localPosition;

        // adjusting target position
        float currentOffsetY = offsetY;
        if (!show ^ player.isOpponent) currentOffsetY *= -1;
        Vector3 targetPosition = startingPosition + new Vector3(0, currentOffsetY, 0);
        float t = 0;
        float timeAppearing = 0.4f;

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
    IEnumerator TokenUpdateAnim(FoodType type)
    {
        yield return StartCoroutine(ShowTokens(true));
        foodCounters[(int)type].RefreshToken(true);

        yield return new WaitForSeconds(tokenStayTime);
        yield return StartCoroutine(ShowTokens(false));
    }
}
