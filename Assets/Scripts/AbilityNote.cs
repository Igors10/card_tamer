using UnityEngine;
using TMPro;
public class AbilityNote : MonoBehaviour 
{
    public AbilityObj storedAbility;
    public Color storedAbilityColor;
    [SerializeField] TextMeshProUGUI noteText;

    public void InitAbilityNote(AbilityObj ability)
    {
        // ability color
        storedAbilityColor = Colors.instance.GetRandomSecondaryColor();

        // ability and desc
        storedAbility = ability;
        noteText.text = ability.GetAbilityDesc(storedAbilityColor);
    }
}
