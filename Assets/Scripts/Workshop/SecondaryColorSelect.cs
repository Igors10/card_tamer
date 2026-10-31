using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SecondaryColorSelect : MonoBehaviour
{
    [Header("secondary colors")]
    [SerializeField] Image playerColorPencil;
    [SerializeField] List<PencilButton> secondaryColorChoiceList = new List<PencilButton>();

    private void OnEnable()
    {
        InitSecondaryPencils();
    }
    void InitSecondaryPencils()
    {
        // coloring the left pencil player color
        playerColorPencil.color = GameManager.instance.player.playerColor;

        // coloring the color choices secondary colors
        for (int i = 0; i < secondaryColorChoiceList.Count; i++)
        {
            secondaryColorChoiceList[i].SetColor(Colors.instance.secondaryColorList[i]);
        }
    }

    public void PickColor(Image pickedPencilImage)
    {
        Color pickedColor = pickedPencilImage.color;
        GameManager.instance.managerUI.workshop.PickSecondaryColor(pickedColor);
    }
}
