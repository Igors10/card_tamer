using UnityEngine;

public class Colors : MonoBehaviour
{
    public static Colors instance;
    const float secDarkColorCoof = 0.65f;
    const float secLightColorCoof = 0.55f;
    public Color buffColor;
    public Color debuffColor;

    void Awake()
    {
        instance = this;
    }

    // Whitens or darkens chosen color
    public Color BlendColor(Color color, float blendCoof, bool blendWhite = true)
    {
        // deciding on the color to blend with
        Color colorToBlendWith = (blendWhite) ? Color.white : Color.black;

        Color colorToReturn = Color.Lerp(colorToBlendWith, color, blendCoof);
        return colorToReturn;
    }

    public Color GetDarkenColor(Color color) => BlendColor(color, secDarkColorCoof, false);
    public Color GetLightenColor(Color color) => Lighten(color);

    /// <summary>
    /// Marker (second) color made from a player color, usable without a Colors object (main menu)
    /// </summary>
    public static Color Lighten(Color color) => Color.Lerp(Color.white, color, secLightColorCoof);
}
