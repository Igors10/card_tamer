using UnityEngine;

public class DeathParticle : MonoBehaviour
{
    [Header("refs")]
    [HideInInspector] public Player player;
    [SerializeField] SpriteRenderer unitShadow;

    [Header("animaiton")]
    [SerializeField] float shakeLength;
    [SerializeField] float shakeIntensity;


    private void OnEnable()
    {
        GameManager.OnStateTransition += SelfDestruct;
    }

    private void OnDisable()
    {
        GameManager.OnStateTransition -= SelfDestruct;
    }

    public void Init(Sprite sprite, RectTransform imageRect, Player hostPlayer)
    {
        // assigning values
        unitShadow.sprite = sprite;
        player = hostPlayer;

        // match the sprite size
        MatchSize(imageRect);

        // triggering shaking
        Animations.instance.ShakeAnim(unitShadow.gameObject, shakeLength, shakeIntensity);
    }

    void MatchSize(RectTransform imageRect)
    {
        Vector2 imageWorldSize = new Vector2(
             imageRect.rect.width * imageRect.lossyScale.x,
             imageRect.rect.height * imageRect.lossyScale.y
         );

        // 2. Get the natural world size of the Sprite right now (before scaling)
        // Sprite size in units = texture pixels / PPU
        Vector2 spriteNaturalSize = unitShadow.sprite.rect.size / unitShadow.sprite.pixelsPerUnit;

        // 3. Calculate the exact scale needed to match the Image
        Vector3 newScale = new Vector3(
            imageWorldSize.x / spriteNaturalSize.x,
            imageWorldSize.y / spriteNaturalSize.y,
            1f
        );

        // 5. Apply the scale to the SpriteRenderer's transform
        unitShadow.transform.localScale = newScale;
    }

    void SelfDestruct()
    {
        // destroying the particle
        Destroy(this.gameObject);
    }
}
