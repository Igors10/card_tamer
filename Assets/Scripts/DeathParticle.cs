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
        // 1. Get the pixel dimensions of the UI Image
        Vector2 imageSizeInPixels = imageRect.rect.size;

        // 2. Get the Pixels Per Unit (PPU) of the sprite being used
        float spritePPU = unitShadow.sprite.pixelsPerUnit;

        // 3. Calculate the required world scale
        // Formula: Target Pixel Size / Sprite PPU
        Vector3 newScale = new Vector3(
            imageSizeInPixels.x / spritePPU,
            imageSizeInPixels.y / spritePPU,
            1f
        );

        // 4. Handle UI Canvas scaling if necessary
        // If your Canvas is set to "Scale With Screen Size", we must account for its local scale.
        Canvas canvas = imageRect.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            newScale.x *= canvas.transform.localScale.x;
            newScale.y *= canvas.transform.localScale.y;
        }

        // 5. Apply the scale to the SpriteRenderer's transform
        unitShadow.transform.localScale = newScale;
    }

    void SelfDestruct()
    {
        // destroying the particle
        Destroy(this.gameObject);
    }
}
