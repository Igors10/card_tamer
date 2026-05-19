using UnityEditor.ShaderGraph.Internal;
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

    public void Init(Sprite sprite, Player hostPlayer)
    {
        // assigning values
        unitShadow.sprite = sprite;
        player = hostPlayer;

        // triggering shaking
        Animations.instance.ShakeAnim(unitShadow.gameObject, shakeLength, shakeIntensity);
    }

    void SelfDestruct()
    {
        // destroying the particle
        Destroy(this.gameObject);
    }
}
