using UnityEngine;
using System.Collections.Generic;

public class Colors : MonoBehaviour
{
    public static Colors instance;
    public List<Color> secondaryColorList = new List<Color>();
    public int secondaryColorAmount = 3;
    public List<Color> allColorList = new List<Color>();
    float secDarkColorCoof = 0.65f;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        allColorList = GameManager.instance.cardDatabase.allPlayerColors;
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
    

    /// <summary>
    /// Picks 3 random colors that neither player has as their primary color
    /// </summary>
    public void GenerateRandomSecondaryColors()
    {
        for (int i = 0; i < secondaryColorAmount; i++)
        {
            // picking random color from the list
            int randomColorID = 0;

            do
            {
                randomColorID = Random.Range(0, allColorList.Count);
            } while (ColorAlreadyInUse(allColorList[randomColorID]));

            // if it's not in use add it to the secondary list  color
            secondaryColorList.Add(allColorList[randomColorID]);

            Debug.Log("Colors: new secondary color -> " + secondaryColorList[i]);
        }
    }

    public int GetSecondaryColorID(Color colorToIdentify)
    {
        int secondaryColorID = 0;

        for (int i = 0; i < secondaryColorList.Count; i++)
        {
            if (colorToIdentify == secondaryColorList[i]) secondaryColorID = i;
        }
        return secondaryColorID;
    }

    /// <summary>
    /// Checks if color is already used somewhere
    /// </summary>
    /// <param name="colorToCheck"></param>
    /// <returns></returns>
    bool ColorAlreadyInUse(Color colorToCheck)
    {
        // checking if color is already a main color of one of the players
        if (GameManager.instance.player.playerColor == colorToCheck) return true;
        else if (GameManager.instance.opponent.playerColor == colorToCheck) return true;

        // checking if the color is already one of the secondary colors
        for (int a = 0; a < secondaryColorList.Count; a++)
        {
            if (colorToCheck == secondaryColorList[a]) return true;
        }

        return false;
    }

    public Color GetRandomSecondaryColor() => secondaryColorList[Random.Range(0, secondaryColorList.Count)];
}
