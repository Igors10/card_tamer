using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] Image pencil;
    [SerializeField] GameObject colorWindow;
    [SerializeField] Button[] colorButtons;
    [SerializeField] Material selectMaterial;
    Material defaultMaterial;

    [Header("Colors")]
    [SerializeField] Color[] colors;

    void Start()
    {
        InitColorWindow();

        // initializing default player color here as well
        TitleScreen.instance.playerConfigObj.playerColor = colors[0];

        // making the pencil reflect player color
        pencil.color = TitleScreen.instance.playerConfigObj.playerColor;
    }
    void InitColorWindow()
    {
        for (int i = 0; i < colorButtons.Length; i++)
        {
            if (i >= colors.Length) return;

            colorButtons[i].GetComponent<Image>().color = colors[i];

            Color colorToSelect = colors[i];
            colorButtons[i].onClick.AddListener(() => SelectColor(colorToSelect));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenColorWindow();
    }

    void OnHover(bool isHovered)
    {
        // green select outline

        pencil.material = (isHovered || colorWindow.activeSelf) ? selectMaterial : defaultMaterial;
    }

    void OpenColorWindow()
    {
        colorWindow.SetActive(true);
    }

    public void SelectColor(Color colorToSelect)
    {
        pencil.color = colorToSelect;
        TitleScreen.instance.playerConfigObj.playerColor = colorToSelect;

        // closing the window
        colorWindow.SetActive(false);
        OnHover(false);
    }
}
