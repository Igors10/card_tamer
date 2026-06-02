using UnityEngine;

public class Colors : MonoBehaviour
{
    public static Colors instance;

    void Awake()
    {
        instance = this;
    }

    // Blends provided color with white
    public Color BlendColor(Color color, float blendCoof)
    {
        Color colorToReturn = Color.Lerp(Color.white, color, blendCoof);
        return colorToReturn;
    }
}
