using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class FrameAnim : MonoBehaviour
{
    [Header("refs")]
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Image image;

    [Header("Animation settings")]
    [SerializeField] Sprite[] animSprites;
    [SerializeField] int animIntervals;
    public bool pauseAnim = false;

    int currentFrame = 0;
    float t = 0;

    private void FixedUpdate()
    {
        Animation();
    }


    /// <summary>
    /// Loops through sprites if not paused
    /// </summary>
    void Animation()
    {
        if (!pauseAnim)
        {
            t++;

            if (t >= animIntervals)
            {
                currentFrame = (currentFrame + 1) % animSprites.Length;
                SetSprite(currentFrame);
            }
        }
    }

    public void PauseAnimation(bool pause)
    {
        pauseAnim = pause;
        
        currentFrame = 0;
        SetSprite(currentFrame);
    }

    void SetSprite(int frameNr)
    {
        t = 0;

        if (sprite != null) sprite.sprite = animSprites[frameNr];
        else image.sprite = animSprites[frameNr];
    }
}
