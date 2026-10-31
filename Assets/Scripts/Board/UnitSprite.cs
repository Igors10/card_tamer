using UnityEngine;
using UnityEngine.UI;

public class UnitSprite : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] Image[] spriteArray;
    [SerializeField] Image[] materialSpriteArray;
 
    /// <summary>
    /// Updates the visuals of combined sprite
    /// </summary>
    /// <param name="sprites"></param>
    /// <param name="primaryColor"></param>
    /// <param name="secondaryColor"></param>
    public void RefreshSprite(Sprite[] sprites, Color primaryColor, Color secondaryColor)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            // disabling the image component if there's no sprite for current layer
            spriteArray[i].enabled = sprites[i] != null;
            materialSpriteArray[i].enabled = sprites[i] != null; 

            spriteArray[i].sprite = sprites[i];
            materialSpriteArray[i].sprite = sprites[i];
            spriteArray[i].color = (i == 0) ? primaryColor : secondaryColor;
        }
    }

    public void RefreshColor(Color primaryColor, Color secondaryColor)
    {
        spriteArray[0].color = primaryColor;
        spriteArray[1].color = secondaryColor;
    }

    public void RefreshSpriteMaterial(Material newMaterial)
    {
        for (int i = 0; i < spriteArray.Length; i++)
        {
            materialSpriteArray[i].material = newMaterial;
        }
    }

    public void SetSpriteAlpha(float newAlpha)
    {
        for (int i = 0; i < spriteArray.Length; i++)
        {
            Color prevColor = spriteArray[i].color;
            Color newColor = new Color(prevColor.r, prevColor.g, prevColor.b, newAlpha);
            spriteArray[i].color = newColor;
        }
    }
}
