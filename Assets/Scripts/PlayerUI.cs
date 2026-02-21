using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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


    private void Start()
    {
        player = GetComponent<Player>();
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
}
