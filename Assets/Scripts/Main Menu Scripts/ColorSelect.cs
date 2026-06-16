using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class ColorSelect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] Image pencil;
    [SerializeField] GameObject colorWindow;
    [SerializeField] Button[] colorButtons;
    [SerializeField] Material selectMaterial;
    Material defaultMaterial;

    [Header("color select pointer")]
    [SerializeField] Image colorSelectPointer;
    [SerializeField] Sprite[] pointerFrames;
    [SerializeField] float pointerAnimIntervals;

    [Header("Colors")]
    [SerializeField] Color[] colors;

    void Start()
    {
        InitColorWindow();

        // initializing default player color here as well
        TitleScreen.instance.playerConfigObj.playerColor = colors[0];
        // starting the pointer animation
        StartCoroutine(PointerAnim());

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

    /// <summary>
    /// Four arrows pointing at the pencil so that player knows its interactable. Arrows disappear until the end of scene after player hovers over the pencil
    /// </summary>
    /// <returns></returns>
    IEnumerator PointerAnim()
    {
        colorSelectPointer.gameObject.SetActive(true);
        float t = 0;

        while (colorSelectPointer.gameObject.activeSelf)
        {
            t += Time.deltaTime;

            if (t > pointerAnimIntervals)
            {
                Debug.Log("ColorSelect: pointer frame change");
                t = 0;
                colorSelectPointer.sprite = (colorSelectPointer.sprite == pointerFrames[0]) ? pointerFrames[1] : pointerFrames[0];
            }
            yield return null;
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

        // disabling pointers
        colorSelectPointer.gameObject.SetActive(false);
    }

    void OpenColorWindow()
    {
        colorWindow.SetActive(true);
    }

    public void SelectColor(Color colorToSelect)
    {
        pencil.color = colorToSelect;
        TitleScreen.instance.playerConfigObj.playerColor = colorToSelect;

        // playing soundeffect
        AudioManager.instance.PlaySFX("ColorPickSFX");

        // closing the window
        colorWindow.SetActive(false);
        OnHover(false);
    }
}
