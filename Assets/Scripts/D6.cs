using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class D6 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("refs")]
    [SerializeField] PowerCounter powerCounter;
    [SerializeField] Image sprite;
    [SerializeField] Sprite[] diceFaces;
    [SerializeField] GameObject glow;

    [Header("rolling")]
    bool rolling = false;
    int diceValue = 1;
    [SerializeField] float rollingTime;
    [SerializeField] float diceIntervals;
    [SerializeField] float rollDistance;
    [SerializeField] float rotationSpeed;

    private void Start()
    {
        sprite.color = powerCounter.player.playerColor;
    }
    public IEnumerator RollAnimation()
    {
        Vector3 diceStartingPos = transform.position;
        Vector3 diceTargetPos = new Vector3(diceStartingPos.x, diceStartingPos.y + rollDistance, diceStartingPos.z);
        rolling = true;

        // Floating up
        float t = 0;
        while (t < rollingTime)
        {
            t += Time.deltaTime;
            float actualT = t / rollingTime;
            float coolT = actualT * actualT;

            transform.position = Vector3.Lerp(diceStartingPos, diceTargetPos, coolT);

            diceValue = ChangeDiceValue();
            yield return null;
            //yield return new WaitForSeconds(diceIntervals);
        }
        // Floating down
        t = 0;
        while (t < rollingTime)
        {
            t += Time.deltaTime;
            float actualT = t / rollingTime;
            float coolT = 1 - (1 - actualT) * (1 - actualT);

            transform.position = Vector3.Lerp(diceTargetPos, diceStartingPos, coolT);

            diceValue = ChangeDiceValue();
            yield return null;
            //yield return new WaitForSeconds(diceIntervals);
        }

        rolling = false;
    }

    void RotateDice()
    {
        RectTransform rect = sprite.GetComponent<RectTransform>();

        float rotationThisFrame = (rolling) ? rotationSpeed * Time.deltaTime : 0;
        rect.Rotate(0f, 0f, -rotationThisFrame); 
    }

    void FixedUpdate()
    {
        RotateDice();
    }

    int ChangeDiceValue()
    {
        int newDiceValue = Random.Range(1, 7);
        sprite.sprite = diceFaces[newDiceValue - 1];

        return newDiceValue;
    }

    public int GetDiceValue()
    {
        return diceValue;
    }

    void ManualRoll()
    {
        if (rolling || powerCounter.diceRolled) return;

        glow.SetActive(false);
        StartCoroutine(powerCounter.RollDicePower());
    }

    void OnHover(bool mouseOver)
    {
        if (rolling || powerCounter.diceRolled) return;

        glow.SetActive(mouseOver);
    }

    // ================== MOUSE INPUT =====================
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
        ManualRoll();
    }
}
