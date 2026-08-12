using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitAtDiscard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] Material defaultMaterial;
    [SerializeField] Material selectMaterial;
    [SerializeField] Material discardMaterial;
    public UnitSprite sprite;
    [SerializeField] Image[] unitPiece;
    [SerializeField] CartoonShakeEffect cartoonShakeEffect;
    [SerializeField] HoverEffect hoverEffect;
    [SerializeField] GameObject CutVFX;

    [Header("discard attributes")]
    [SerializeField] Color discardedUnitColor;

    [HideInInspector] public Card storedCard;
    bool discarded = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        TryDiscard();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHover(false);
    }

    void OnHover(bool isHover)
    {
        if (!GameManager.instance.yourTurn || !discarded) return; // only during player's turn

        // color the unit as selectable
        Material newMaterial = (isHover && GameManager.instance.discardManager.discardAvailable) ? selectMaterial : defaultMaterial;
        sprite.RefreshSpriteMaterial(newMaterial);

        // creating card preview above the unit
        Vector3 previewPos = GameManager.instance.discardManager.previewPoint.transform.position;
        GameManager.instance.managerUI.PreviewCard(isHover, storedCard.cardData, storedCard.player, previewPos, false);
    }

    void TryDiscard()
    {
        // can only discard on player's turn and when units have appeared on the screen
        if (!GameManager.instance.yourTurn || !GameManager.instance.discardManager.discardAvailable || discarded) return;

        // tell discard manager that you want to discard this unit
        GameManager.instance.discardManager.DiscardUnit(this);
    }

    void UnitDeathAnim()
    {
        // VFX
        //ParticleManager.instance.SpawnVFX(transform.position, "HitVFX", true); // doesnt work on a canvas
        CutVFX.SetActive(true);

        // SFX
        AudioManager.instance.PlaySFX("UnitDeathSFX");
    }

    /// <summary>
    /// Indicates that unit is discarded (or reverses it if false)
    /// </summary>
    /// <param name="isDiscarded"></param>
    public void Discard(bool isDiscarded)
    {
        discarded = isDiscarded;

        if (discarded) UnitDeathAnim();

        // Making unit appear cut in half
        /*
        for (int i = 0; i < unitPiece.Length; i++)
        {
            unitPiece[i].gameObject.SetActive(isDiscarded);
            //unitPiece[i].sprite = sprite.sprite;
        }*/

        // chaning the material
        Material material = (isDiscarded) ? discardMaterial : defaultMaterial; 
        sprite.RefreshSpriteMaterial(material);

        // changing the color
        Color primColor = (isDiscarded) ? discardedUnitColor : storedCard.player.playerColor;
        Color secColor = (isDiscarded) ? discardedUnitColor : storedCard.cardData.secondaryColor;
        sprite.RefreshColor(primColor, secColor);

        // disabling cartoon shake animation and hovering functionality
        cartoonShakeEffect.enabled = !isDiscarded;
        hoverEffect.enabled = !isDiscarded;
    }
}
