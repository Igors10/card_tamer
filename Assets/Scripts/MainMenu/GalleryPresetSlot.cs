using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One drawing in the main menu gallery: the doodle, its name (renamed in place) and a delete button
/// </summary>
public class GalleryPresetSlot : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] UnitSprite unitSprite;
    [SerializeField] TMP_InputField nameField;
    [SerializeField] Button renameButton;
    [SerializeField] Button deleteButton;

    GalleryMenu galleryMenu;
    UnitPreset preset;

    public void Init(GalleryMenu menu)
    {
        galleryMenu = menu;
        nameField.characterLimit = GalleryMenu.maxNameLength;
        // readOnly instead of interactable: turning interactable off while the field is losing focus
        // (clicking rename again) makes Unity change the selection mid-selection, which logs an error
        nameField.readOnly = true;
        nameField.onEndEdit.AddListener(FinishRename);
        nameField.onSelect.AddListener(_ => nameField.readOnly = false); // clicking the name renames too
        renameButton.onClick.AddListener(StartRename);
        deleteButton.onClick.AddListener(() => galleryMenu.DeletePreset(preset));
    }

    /// <summary>
    /// Shows a drawing, or hides the slot when there's none
    /// </summary>
    public void Show(UnitPreset newPreset, Color primaryColor, Color secondaryColor)
    {
        preset = newPreset;
        gameObject.SetActive(preset != null);
        if (preset == null) return;

        nameField.readOnly = true;
        nameField.text = preset.unitName;
        unitSprite.RefreshSprite(preset.sprite, primaryColor, secondaryColor);
    }

    public void RefreshColor(Color primaryColor, Color secondaryColor) => unitSprite.RefreshColor(primaryColor, secondaryColor);

    void StartRename()
    {
        AudioManager.instance.PlaySFX("ButtonSFX");
        nameField.readOnly = false;
        nameField.ActivateInputField();
        nameField.MoveTextEnd(false);
    }

    void FinishRename(string newName)
    {
        nameField.readOnly = true;
        newName = newName.Trim();

        // empty or unchanged name keeps the old one
        if (newName == "" || newName == preset.unitName) { nameField.text = preset.unitName; return; }

        Gallery.Rename(preset, newName);
    }
}
