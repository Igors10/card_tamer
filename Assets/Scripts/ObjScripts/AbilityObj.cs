using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ability")]
public class AbilityObj : ScriptableObject
{
    public int power;
    public string abilityDescription;
    public bool isSpecial;

    public GameObject effect;
    public GameObject highlightCon; // highlight condition

    public string GetAbilityDesc(Color abilityColor)
    {
        string abilityText = abilityDescription;

        // coloring the power modifier numbers red
        string pattern = @"[\+\-]\d+";
        abilityText = Regex.Replace(abilityText, pattern, "<color=red>$0</color>");

        // coloring the 'color' of abilities that work with specific color
        string hexColor = ColorUtility.ToHtmlStringRGB(abilityColor);
        abilityText = abilityText.Replace("{color}", $"<color=#{hexColor}>color</color>");

        return abilityText;
    }
}
