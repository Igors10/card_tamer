using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One of the stickers offered in the shop. Clicking it buys the sticker, then the player clicks a card in hand to put it on
/// </summary>
public class ShopSticker : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] Ability stickerStrip;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] float stickerHeight = 54f;
    [SerializeField] float stickerFontSize = 24f;
    [SerializeField] float dimmedAlpha = 0.4f; // too expensive stickers fade to this
    CanvasGroup group;

    public Sticker Offered { get; private set; } // null once it's bought
    public Button Button => button;
    public TextMeshProUGUI PriceText => priceText;

    /// <summary>
    /// Shows a new sticker in the slot (null = empty slot)
    /// </summary>
    public void Show(Sticker sticker)
    {
        Offered = sticker;
        stickerStrip.InitSticker(sticker);
        if (sticker != null) stickerStrip.SetSize(stickerHeight, stickerFontSize);
        priceText.gameObject.SetActive(sticker != null);
    }

    /// <summary>
    /// Fades the button and the sticker when the player can't afford it (the price and its star stay clear)
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        if (group == null)
        {
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
            // the price has its own group that ignores the fade
            if (!priceText.TryGetComponent(out CanvasGroup priceGroup)) priceGroup = priceText.gameObject.AddComponent<CanvasGroup>();
            priceGroup.ignoreParentGroups = true;
        }
        group.alpha = dimmed ? dimmedAlpha : 1f;
    }
}
