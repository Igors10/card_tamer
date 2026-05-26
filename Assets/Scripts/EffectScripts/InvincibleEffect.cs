using UnityEngine;

public class InvincibleEffect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Effect effect = GetComponent<Effect>();
        effect.unit.card.shielded = true;
    }
}
