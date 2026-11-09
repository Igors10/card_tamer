using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One sticker strip on a card. Card has a strip for each sticker slot, empty slots are hidden
/// </summary>
public class Ability : MonoBehaviour
{
    [Header("AbilityData")]
    public AbilityObj abilityData;
    [HideInInspector] public Card card;
    public Color abilityColor;
    [HideInInspector] public bool onRight; // left or right version of the sticker

    [Header("refs")]
    [SerializeField] TextMeshProUGUI effectDesc;
    [SerializeField] Image stickerBG; // tinted with the sticker's color
    [SerializeField] GameObject specialStar; // special stickers have a star behind them
    [SerializeField] Image icon; // sticker icon on the left of the description
    [SerializeField] GameObject glow; // lights up behind the strip while its effect triggers
    [SerializeField] float specialScale = 1.1f; // special stickers are a bit bigger
    Vector3 restScale;
    bool restScaleSaved;

    // ====================
    // Initialization
    // ====================

    /// <summary>
    /// Applying sticker data to this strip, null hides the strip
    /// </summary>
    public void InitAbility(Sticker sticker, Card cardAssignTo)
    {
        card = cardAssignTo;
        abilityData = sticker?.ability;
        gameObject.SetActive(abilityData != null);
        if (abilityData == null) return;

        onRight = sticker.onRight;

        // Ability color
        abilityColor = card.cardData.abilityColor;

        // EFFECT DESCRIPTION
        effectDesc.text = abilityData.GetAbilityDesc(abilityColor, onRight);

        // sticker look
        if (stickerBG != null) stickerBG.color = abilityData.stickerColor;
        if (specialStar != null) specialStar.SetActive(abilityData.isSpecial);
        if (icon != null)
        {
            icon.gameObject.SetActive(abilityData.icon != null);
            icon.sprite = abilityData.icon;
            icon.color = abilityData.IconColor;
        }

        if (!restScaleSaved) { restScale = transform.localScale; restScaleSaved = true; }
        transform.localScale = (abilityData.isSpecial) ? restScale * specialScale : restScale;
    }

    /// <summary>
    /// Strip height (cards with fewer stickers get bigger strips and bigger text)
    /// </summary>
    public void SetSize(float height, float maxFontSize)
    {
        RectTransform rt = (RectTransform)transform;
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        effectDesc.fontSizeMax = maxFontSize;
        if (icon != null)
        {
            float iconSize = Mathf.Min(height * 0.85f, 64f);
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.rectTransform.anchoredPosition = new Vector2(iconSize / 2f + 4f, 0f);
            effectDesc.rectTransform.offsetMin = new Vector2(iconSize + 8f, effectDesc.rectTransform.offsetMin.y);
        }
    }

    /// <summary>
    /// The strip lights up on the card while its effect triggers
    /// </summary>
    public void Highlight(bool isOn)
    {
        if (glow != null) glow.SetActive(isOn);
        if (isOn) Animations.instance.PopAnim(gameObject, 0.3f, 0.06f);
    }

    /// <summary>
    /// Description with the right side filled in
    /// </summary>
    public string GetDescription() => abilityData.GetAbilityDesc(abilityColor, onRight);

    /// <summary>
    /// Shows other text on the strip instead of the description (the effect notifier says what the sticker is doing)
    /// </summary>
    public void SetText(string text) => effectDesc.text = text;

    // =====================
    // Effect
    // =====================

    /// <summary>
    /// Applies the effect of the ability to the unit(s)
    /// </summary>
    public void UseAbility()
    {
        Debug.Log("Ability: " + card.name + " uses " + abilityData.name);

        // playing soundeffect
        AudioManager.instance.PlaySFX("UseAbilitySFX");

        // Effect
        GameManager.instance.executeManager.CardUseAbl(card);
    }
}
