using UnityEngine;
using TMPro;
public class AbilityNote : MonoBehaviour 
{
    public AbilityObj storedAbility;
    [SerializeField] TextMeshProUGUI noteText;

    public void InitAbilityNote(AbilityObj ability)
    {
        storedAbility = ability;
        noteText.text = ability.abilityDescription;
    }
}
