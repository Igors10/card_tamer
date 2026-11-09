using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class AbilityNote : MonoBehaviour
{
    public AbilityObj storedAbility;
    public Color storedAbilityColor;
    [HideInInspector] public bool storedOnRight; // side of the offered sticker
    [SerializeField] Image stickerBG; // tinted with the sticker's color
    [SerializeField] Image icon; // sticker icon (in place of the old power star)
    Vector3 iconRestPos;
    bool iconRestSaved;
    [SerializeField] TextMeshProUGUI noteText;
    [SerializeField] TextMeshProUGUI powerValue;
    [SerializeField] Transform powerObj;
    [SerializeField] Image powerBG;
    [SerializeField] Color specialPowerColor = new Color(0.78f, 0.55f, 0.96f);
    Color defaultPowerColor;
    bool defaultPowerColorSaved = false;

    [Header("no description layout")]
    [SerializeField] Vector3 centeredPowerPos = Vector3.zero; // power takes the middle of the note when there's no description
    [SerializeField] float centeredPowerScaleMod = 1.6f;
    Vector3 powerRestPos;
    Vector3 powerRestScale;
    bool restSaved;

    /// <summary>
    /// Shows the sticker on the note: icon, color and description (cards have no base power, so no power is shown)
    /// </summary>
    public void InitAbilityNote(Sticker sticker)
    {
        // the sticker icon sits in the middle on top of the note
        if (!iconRestSaved) { iconRestPos = icon.rectTransform.localPosition; iconRestSaved = true; }
        icon.sprite = sticker.ability.icon;
        icon.color = sticker.ability.IconColor;
        icon.gameObject.SetActive(sticker.ability.icon != null);
        icon.rectTransform.localPosition = Vector3.zero;
        if (powerBG != null) powerBG.gameObject.SetActive(false);
        else powerValue.gameObject.SetActive(false);
        powerObj.gameObject.SetActive(true);

        // ability color
        //storedAbilityColor = Colors.instance.GetRandomSecondaryColor();

        // ability and desc
        AbilityObj ability = sticker.ability;
        storedAbility = ability;
        storedOnRight = sticker.onRight;
        noteText.text = ability.GetAbilityDesc(storedAbilityColor, storedOnRight);
        if (stickerBG != null) stickerBG.color = ability.stickerColor;

        powerValue.text = ability.GetPowerText();
        FitPowerToDescription(!string.IsNullOrEmpty(ability.abilityDescription));

        RefreshPowerColor(ability.isSpecial);
    }

    /// <summary>
    /// Special abilities have a purple power counter
    /// </summary>
    public void RefreshPowerColor(bool isSpecial)
    {
        if (powerBG == null) return;

        if (!defaultPowerColorSaved) { defaultPowerColor = powerBG.color; defaultPowerColorSaved = true; }
        powerBG.color = (isSpecial) ? specialPowerColor : defaultPowerColor;
    }

    /// <summary>
    /// Abilities without a description (like the default roll) get a bigger power value in the middle of the note
    /// </summary>
    public void FitPowerToDescription(bool hasDescription)
    {
        if (!restSaved)
        {
            powerRestPos = powerObj.localPosition;
            powerRestScale = powerObj.localScale;
            restSaved = true;
        }

        noteText.gameObject.SetActive(hasDescription);
        powerObj.localPosition = hasDescription ? powerRestPos : centeredPowerPos;
        powerObj.localScale = hasDescription ? powerRestScale : new Vector3(powerRestScale.x * centeredPowerScaleMod, powerRestScale.y * centeredPowerScaleMod, powerRestScale.z);
    }
}
