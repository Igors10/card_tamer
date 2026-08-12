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

        // spawning highlight condition object
        if (abilityData.highlightCon != null) Instantiate(abilityData.highlightCon, card.transform);
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

        // effect prefab
        if (abilityData.effect != null) Instantiate(abilityData.effect, card.unit.transform);
    }
}
