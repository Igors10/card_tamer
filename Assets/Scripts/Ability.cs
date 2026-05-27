using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Ability : MonoBehaviour
{
    [Header("AbilityData")]
    public AbilityObj abilityData;
    [HideInInspector] public Card card;

    [Header("refs")]
    [SerializeField] TextMeshProUGUI name;
    public GameObject powerIcon;
    [SerializeField] TextMeshProUGUI powerValue;
    [SerializeField] TextMeshProUGUI effectDesc;
    [SerializeField] Image background;

    [Header("Effect text params")]
    [SerializeField] float defaultEffectX;
    [SerializeField] float defaultEffectWidth;
    [SerializeField] float noPowerEffectX;
    [SerializeField] float noPowerEffectWidth;
    [SerializeField] float passiveEffectX;
    [SerializeField] float passiveEffectWidth;

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

        // POWER
        /*powerIcon.SetActive(abilityData.power != 0);
        powerValue.text = (abilityData.power > 0) ? "+" + abilityData.power.ToString() : abilityData.power.ToString();*/

        // EFFECT DESCRIPTION
        effectDesc.text = abilityData.abilityDescription;

        // EFFECT FORMATING
        // text alignment
        /*
        effectDesc.alignment = (abilityData.power != 0) ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;

        // textbox size and position
        float effectX = defaultEffectX;
        float effectWidth = defaultEffectWidth;
        // if no power move effect to the center
        if (abilityData.power == 0)
        {
            effectX = passiveEffectX;
            effectWidth = passiveEffectWidth;
        }
        // if no effect, move power to the center
        else if (abilityData.abilityDescription == "")
        {
            powerIcon.transform.localPosition = new Vector3(passiveEffectX, powerIcon.transform.localPosition.y, 0f);
        }

            // applying formating (making it take as much space as possible if some other elements are disabled)
            effectDesc.transform.localPosition = new Vector3(effectX, effectDesc.transform.localPosition.y, 0f);
        effectDesc.rectTransform.sizeDelta = new Vector2(effectWidth, effectDesc.rectTransform.sizeDelta.y);*/
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
        GameManager.instance.executeManager.CardUseAbl(card, this);
        // effect prefab
        if (abilityData.effect != null) Instantiate(abilityData.effect, card.unit.transform);
    }
}
