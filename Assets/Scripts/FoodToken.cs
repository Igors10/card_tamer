using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
public enum FoodType
{
    MEAT,
    FISH,
    BERRIES
}

public class FoodToken : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Player player;
    [SerializeField] Image sprite;
    [SerializeField] TextMeshProUGUI textAmount;
    [SerializeField] ParticleSystem tokenVFX;
    [SerializeField] FoodType type;

    [Header("blinking")]
    [SerializeField] float blinkingTime;
    [SerializeField] float blinkingInterval;
    [SerializeField] Color blinkingColor;

    private void Start()
    {
        RefreshToken(false);
    }
    /// <summary>
    /// Refreshes visuals based om the amount of food token on the player
    /// </summary>
    /// <param name="amount"></param>
    public void RefreshToken(bool withVFX)
    {
        // getting the amount
        int amount = player.food[(int)type];

        // changing text
        textAmount.text = (amount == 0) ? "" : amount.ToString();

        // fading out the sprite if there is no food of this type
        if (amount == 0) sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);
        else sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1f);

        if (withVFX)
        {
            // playing the particle effect
            if (tokenVFX != null) tokenVFX.Play();

            // making the token pop
            GameManager.instance.animations.PopAnim(this.gameObject, 0.5f, 0.45f);
        }
    }


    /// <summary>
    /// Blinking red when not enough of this for buying a card
    /// </summary>
    /// <returns></returns>
    public IEnumerator NegativeBlink()
    {
        float t = 0;
        float intervalT = 0;
        Color startingColor = sprite.color;

        while (t <  blinkingTime)
        {
            t += Time.deltaTime;
            intervalT += Time.deltaTime;

            if (intervalT > blinkingInterval)
            {
                intervalT = 0;

                sprite.color = (sprite.color == blinkingColor) ? startingColor : blinkingColor;
            }
            yield return null;
        }

        sprite.color = startingColor;
    }
}
