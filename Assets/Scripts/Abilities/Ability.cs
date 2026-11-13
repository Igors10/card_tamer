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

    [Header("power numbers in the description (+X and -X get an outline)")]
    [SerializeField] Color buffOutline = Color.white;
    [SerializeField] Color debuffOutline = new Color(1f, 0.6f, 0.6f);
    [SerializeField, Range(0f, 0.5f)] float outlineWidth = 0.2f;

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
        InitSticker(sticker, card.cardData.abilityColor);
    }

    /// <summary>
    /// Applying sticker data without a card (workshop options and the drawing reminder)
    /// </summary>
    public void InitSticker(Sticker sticker, Color color)
    {
        abilityData = sticker?.ability;
        gameObject.SetActive(abilityData != null);
        if (abilityData == null) return;

        onRight = sticker.onRight;

        // Ability color
        abilityColor = color;

        // EFFECT DESCRIPTION
        effectDesc.extraPadding = true; // room for the outline around the numbers
        effectDesc.text = abilityData.GetAbilityDesc(abilityColor, onRight, OutlineMaterial(buffOutline, "Buff"), OutlineMaterial(debuffOutline, "Debuff"));

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
    /// Name of an outlined copy of the description's font material, for the material tag: "[font material] Buff" or "[font material] Debuff".
    /// TMP finds it by name in its MaterialReferenceManager, the color and width come from this prefab
    /// </summary>
    string OutlineMaterial(Color color, string suffix)
    {
        Material fontMaterial = effectDesc.fontSharedMaterial;
        string materialName = fontMaterial.name + " " + suffix;
        int hash = TMP_TextUtilities.GetHashCode(materialName);

        if (!MaterialReferenceManager.TryGetMaterial(hash, out Material outlined))
        {
            outlined = new Material(fontMaterial) { name = materialName };
            outlined.EnableKeyword(ShaderUtilities.Keyword_Outline);
            MaterialReferenceManager.AddFontMaterial(hash, outlined);
        }
        outlined.SetColor(ShaderUtilities.ID_OutlineColor, color);
        outlined.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        outlined.SetFloat(ShaderUtilities.ID_FaceDilate, outlineWidth); // the outline goes around the letters instead of eating into them
        return materialName;
    }

    /// <summary>
    /// Editor: the outline materials exist outside play mode too, so the tags in the prefab's text show (and follow the color changes)
    /// </summary>
    void OnValidate()
    {
        if (effectDesc == null) return;
        OutlineMaterial(buffOutline, "Buff");
        OutlineMaterial(debuffOutline, "Debuff");
        effectDesc.extraPadding = true;
        effectDesc.SetAllDirty();
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
            float slotSize = Mathf.Min(height * 0.85f, 64f); // icon slot on the left, text starts after it
            float iconSize = Mathf.Min(height * 1.4f, 110f); // icon is bigger than its slot and spills over the edges
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            icon.rectTransform.anchoredPosition = new Vector2(slotSize / 2f + 4f, 0f);
            effectDesc.rectTransform.offsetMin = new Vector2(slotSize + 8f, effectDesc.rectTransform.offsetMin.y);
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
