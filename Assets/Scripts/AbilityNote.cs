using UnityEngine;
using TMPro;
public class AbilityNote : MonoBehaviour 
{
    public AbilityObj storedAbility;
    public Color storedAbilityColor;
    [SerializeField] TextMeshProUGUI noteText;
    [SerializeField] TextMeshProUGUI powerValue;
    [SerializeField] Transform powerObj;

    [Header("no description layout")]
    [SerializeField] Vector3 centeredPowerPos = Vector3.zero; // power takes the middle of the note when there's no description
    [SerializeField] float centeredPowerScaleMod = 1.6f;
    Vector3 powerRestPos;
    Vector3 powerRestScale;
    bool restSaved;

    public void InitAbilityNote(AbilityObj ability)
    {
        // ability color
        //storedAbilityColor = Colors.instance.GetRandomSecondaryColor();

        // ability and desc
        storedAbility = ability;
        noteText.text = ability.GetAbilityDesc(storedAbilityColor);

        powerValue.text = ability.GetPowerText();
        FitPowerToDescription(!string.IsNullOrEmpty(ability.abilityDescription));
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
