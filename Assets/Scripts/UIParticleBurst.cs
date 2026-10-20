using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Particle burst made out of UI images, so it can render on a Screen Space - Overlay canvas (particle systems can't)
/// </summary>
public class UIParticleBurst : MonoBehaviour
{
    [Header("shards (tinted with the burst color)")]
    [SerializeField] Sprite[] shardSprites;
    [SerializeField] int shardCount = 10;
    [SerializeField] Vector2 shardSizeRange = new Vector2(22f, 44f);

    [Header("sparks (stay white)")]
    [SerializeField] Sprite[] sparkSprites;
    [SerializeField] int sparkCount = 6;
    [SerializeField] Vector2 sparkSizeRange = new Vector2(18f, 34f);

    [Header("movement")]
    [SerializeField] Vector2 speedRange = new Vector2(300f, 750f);
    [SerializeField] float upwardBias = 150f; // extra upwards speed so the burst pops up before falling
    [SerializeField] Vector2 lifetimeRange = new Vector2(0.45f, 0.8f);
    [SerializeField] Vector2 spinRange = new Vector2(-720f, 720f);
    [SerializeField] float gravity = 1600f;
    [SerializeField] float drag = 3f;

    [Header("ring")]
    [SerializeField] Sprite ringSprite;
    [SerializeField] float ringStartSize = 40f;
    [SerializeField] float ringEndSize = 240f;
    [SerializeField] float ringTime = 0.3f;

    class Particle
    {
        public RectTransform rt;
        public Image image;
        public Vector2 velocity;
        public float spin;
        public float life;
        public float maxLife;
        public Color color;
    }

    /// <summary>
    /// Spawns a burst at given position. Intensity multiplies the amount of particles
    /// </summary>
    public static void Spawn(UIParticleBurst prefab, Vector3 position, Transform parent, Color color, float intensity = 1f)
    {
        UIParticleBurst burst = Instantiate(prefab, parent);
        burst.transform.position = position;
        burst.transform.SetAsLastSibling(); // rendering above the rest of the UI
        burst.StartCoroutine(burst.Burst(color, intensity));
    }

    IEnumerator Burst(Color color, float intensity)
    {
        // creating particles
        List<Particle> particles = new List<Particle>();
        int shards = Mathf.RoundToInt(shardCount * intensity);
        int sparks = Mathf.RoundToInt(sparkCount * intensity);
        for (int i = 0; i < shards; i++) particles.Add(CreateParticle(shardSprites, shardSizeRange, color));
        for (int i = 0; i < sparks; i++) particles.Add(CreateParticle(sparkSprites, sparkSizeRange, Color.white));

        Image ring = (ringSprite != null) ? CreateImage(ringSprite, Color.white, ringStartSize) : null;

        float duration = Mathf.Max(lifetimeRange.y, ringTime);
        float t = 0;
        while (t < duration)
        {
            float deltaTime = Time.deltaTime;
            t += deltaTime;

            // moving particles
            foreach (Particle p in particles)
            {
                if (p.image == null || p.life >= p.maxLife) continue;

                p.life += deltaTime;
                float lifeT = Mathf.Clamp01(p.life / p.maxLife);

                p.velocity *= Mathf.Exp(-drag * deltaTime);
                p.velocity.y -= gravity * deltaTime;
                p.rt.localPosition += (Vector3)(p.velocity * deltaTime);
                p.rt.Rotate(0f, 0f, p.spin * deltaTime);

                // shrinking and fading out towards the end of life
                p.rt.localScale = Vector3.one * (1 - lifeT * lifeT);
                float alpha = 1 - Mathf.Clamp01((lifeT - 0.6f) / 0.4f);
                p.image.color = new Color(p.color.r, p.color.g, p.color.b, p.color.a * alpha);

                if (lifeT >= 1) p.image.enabled = false;
            }

            // expanding ring
            if (ring != null)
            {
                float ringT = Mathf.Clamp01(t / ringTime);
                float coolT = 1 - (1 - ringT) * (1 - ringT);
                float size = Mathf.Lerp(ringStartSize, ringEndSize, coolT);
                ring.rectTransform.sizeDelta = new Vector2(size, size);
                ring.color = new Color(1f, 1f, 1f, 1 - ringT);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    Particle CreateParticle(Sprite[] sprites, Vector2 sizeRange, Color color)
    {
        Sprite sprite = (sprites != null && sprites.Length > 0) ? sprites[Random.Range(0, sprites.Length)] : null;
        Image image = CreateImage(sprite, color, Random.Range(sizeRange.x, sizeRange.y));
        image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(speedRange.x, speedRange.y);
        velocity.y += upwardBias;

        return new Particle
        {
            rt = image.rectTransform,
            image = image,
            velocity = velocity,
            spin = Random.Range(spinRange.x, spinRange.y),
            maxLife = Random.Range(lifetimeRange.x, lifetimeRange.y),
            color = color
        };
    }

    Image CreateImage(Sprite sprite, Color color, float size)
    {
        GameObject obj = new GameObject("particle", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }
}
