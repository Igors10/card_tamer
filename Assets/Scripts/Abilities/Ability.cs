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

        if (!restScaleSaved) { restScale = transform.localScale; restScaleSaved = true; }
        transform.localScale = (abilityData.isSpecial) ? restScale * specialScale : restScale;
    }

    /// <summary>
    /// Description with the right side filled in
    /// </summary>
    public string GetDescription() => abilityData.GetAbilityDesc(abilityColor, onRight);

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
