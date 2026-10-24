using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Ability : MonoBehaviour
{
    [Header("AbilityData")]
    public AbilityObj abilityData;
    [HideInInspector] public Card card;
    public Color abilityColor;

    [Header("refs")]
    [SerializeField] TextMeshProUGUI effectDesc;
    [SerializeField] TextMeshProUGUI powerText;
    [SerializeField] Image powerBG;
    [SerializeField] Color specialPowerColor = new Color(0.78f, 0.55f, 0.96f);
    Color defaultPowerColor;
    bool defaultPowerColorSaved = false;

    // ====================
    // Initialization
    // ====================

    /// <summary>
    /// Applying data from ability scriptable object to this ability instance
    /// </summary>
    public void InitAbility(AbilityObj ability, Card cardAssignTo)
    {
        abilityData = ability;

        // Getting reference to the card
        card = cardAssignTo;

        // Ability color
        abilityColor = card.cardData.abilityColor;

        // EFFECT DESCRIPTION
        effectDesc.text = abilityData.GetAbilityDesc(abilityColor);

        // Power
        powerText.text = abilityData.GetPowerText();

        // special cards have a purple power counter (saved lazily, opponent cards can be initialized before Awake)
        if (powerBG != null)
        {
            if (!defaultPowerColorSaved) { defaultPowerColor = powerBG.color; defaultPowerColorSaved = true; }
            powerBG.color = (abilityData.isSpecial) ? specialPowerColor : defaultPowerColor;
        }
    }

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
