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

    public Sticker Offered { get; private set; } // null once it's bought
    public Button Button => button;
    public TextMeshProUGUI PriceText => priceText;

    /// <summary>
    /// Shows a new sticker in the slot (null = empty slot)
    /// </summary>
    public void Show(Sticker sticker)
    {
        Offered = sticker;
        stickerStrip.InitSticker(sticker, default);
        if (sticker != null) stickerStrip.SetSize(stickerHeight, stickerFontSize);
        priceText.gameObject.SetActive(sticker != null);
    }
}
