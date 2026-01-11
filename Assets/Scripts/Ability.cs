using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Ability : MonoBehaviour
{
    [Header("AbilityData")]
    public AbilityObj abilityData;

    [Header("Attributes")]
    bool active;
    [SerializeField] TextMeshProUGUI name;
    [SerializeField] GameObject speedIcon;
    [SerializeField] GameObject powerIcon;
    [SerializeField] TextMeshProUGUI speedValue;
    [SerializeField] TextMeshProUGUI powerValue;
    [SerializeField] TextMeshProUGUI effectDesc;

    [Header("refs")]
    [SerializeField] Image background;

    [Header("Effect text params")]
    [SerializeField] float defaultEffectX;
    [SerializeField] float defaultEffectWidth;
    [SerializeField] float noPowerEffectX;
    [SerializeField] float noPowerEffectWidth;
    [SerializeField] float passiveEffectX;
    [SerializeField] float passiveEffectWidth;
    
    /// <summary>
    /// Applying data from ability scriptable object to this ability instance
    /// </summary>
    public void InitAbility(AbilityObj ability)
    {
        abilityData = ability;

        // NAME
        name.text = abilityData.name;

        // BACKGROUND
        background.enabled = !abilityData.isPassive; // passive abilities can be identified by not having a colored background

        // SPEED
        speedIcon.SetActive(abilityData.isPassive == false);
        speedValue.text = abilityData.speed.ToString();

        // POWER
        powerIcon.SetActive(abilityData.power > 0 && abilityData.isPassive == false);
        powerValue.text = abilityData.power.ToString();

        // EFFECT DESCRIPTION
        effectDesc.text = abilityData.abilityDescription;

        // EFFECT FORMATING
        // text alignment
        effectDesc.alignment = (abilityData.isPassive) ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;

        // textbox size and position
        float effectX = defaultEffectX;
        float effectWidth = defaultEffectWidth;
        // * (passive ability)
        if (abilityData.isPassive)
        {
            effectX = passiveEffectX;
            effectWidth = passiveEffectWidth;
        }
        // * (no power ability)
        else if (abilityData.power <= 0)
        {
            effectX = noPowerEffectX;
            effectWidth = noPowerEffectWidth;
        }
  
        // applying formating (making it take as much space as possible if some other elements are disabled)
        effectDesc.transform.localPosition = new Vector3(effectX, effectDesc.transform.localPosition.y, 0f);
        effectDesc.rectTransform.sizeDelta = new Vector2(effectWidth, effectDesc.rectTransform.sizeDelta.y);

    }
}
