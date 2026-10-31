using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu gallery screen: the player's saved drawings, 6 per page, with rename and delete.
/// The pencil recolors all drawings (only a preview, nothing is saved)
/// </summary>
public class GalleryMenu : MonoBehaviour
{
    public const int maxNameLength = 20; // same as the workshop name field

    [Header("refs")]
    [SerializeField] GalleryPresetSlot[] slots; // one page
    [SerializeField] GameObject nextPageButton;
    [SerializeField] GameObject previousPageButton;
    [SerializeField] Button pencilButton;
    [SerializeField] Image pencilColorPart;
    [SerializeField] CardDatabase cardDatabase;

    List<UnitPreset> drawings = new List<UnitPreset>();
    int page;
    int colorIndex;
    bool initialized;

    Color PrimaryColor => cardDatabase.allPlayerColors[colorIndex];
    Color SecondaryColor => Colors.Lighten(PrimaryColor);

    void Init()
    {
        if (initialized) return;
        initialized = true;

        foreach (GalleryPresetSlot slot in slots) slot.Init(this);
        nextPageButton.GetComponent<Button>().onClick.AddListener(() => ChangePage(1));
        previousPageButton.GetComponent<Button>().onClick.AddListener(() => ChangePage(-1));
        pencilButton.onClick.AddListener(NextColor);
        pencilColorPart.color = PrimaryColor;
    }

    /// <summary>
    /// Called when the gallery screen opens, starts from the first page
    /// </summary>
    public void Open()
    {
        Init();
        page = 0;
        drawings = Gallery.GetPlayerDrawings();
        ShowPage();
    }

    int PageCount => Mathf.Max(1, Mathf.CeilToInt(drawings.Count / (float)slots.Length));

    void ShowPage()
    {
        page = Mathf.Clamp(page, 0, PageCount - 1);

        for (int i = 0; i < slots.Length; i++)
        {
            int drawingIndex = page * slots.Length + i;
            slots[i].Show(drawingIndex < drawings.Count ? drawings[drawingIndex] : null, PrimaryColor, SecondaryColor);
        }

        nextPageButton.SetActive(page < PageCount - 1);
        previousPageButton.SetActive(page > 0);
    }

    /// <summary>
    /// +1 next page, -1 previous page (the buttons are hidden on the first and last page)
    /// </summary>
    void ChangePage(int direction)
    {
        AudioManager.instance.PlaySFX("ButtonSFX");
        page += direction;
        ShowPage();
    }

    /// <summary>
    /// Pencil click: next player color, the marker color is a lighter version of it (same as in the workshop)
    /// </summary>
    void NextColor()
    {
        AudioManager.instance.PlaySFX("ButtonSFX");
        colorIndex = (colorIndex + 1) % cardDatabase.allPlayerColors.Count;
        pencilColorPart.color = PrimaryColor;

        foreach (GalleryPresetSlot slot in slots) slot.RefreshColor(PrimaryColor, SecondaryColor);
    }

    public void DeletePreset(UnitPreset preset)
    {
        AudioManager.instance.PlaySFX("ButtonSFX");
        Gallery.Delete(preset);
        drawings.Remove(preset);
        ShowPage();
    }

    public bool HasDrawings => drawings.Count > 0;
}
