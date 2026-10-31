using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class PlanningManager : MonoBehaviour
{
    [Header("refs")]
    public GameObject fieldHand;
    [SerializeField] Transform firstCardPosition;

    [Header("Card placement")]
    [SerializeField] int defaultCardInterval;
    [SerializeField] int cardRotationIntensity;

    /// <summary>
    /// Makes cards appear in the correct order on the UI
    /// </summary>
    public void UpdateFieldHandVisuals(Player player)
    {
        for (int i = 0; i < player.cardsOnField.Count; i++)
        {
            // Activating card
            Card card = player.cardsOnField[i];
            card.gameObject.SetActive(true);

            // Positioning cards
            float cardInterval = (player.cardsOnField.Count < 6) ? defaultCardInterval : defaultCardInterval / player.cardsOnField.Count * 5;
            card.transform.position = firstCardPosition.position;
            card.transform.position += new Vector3(cardInterval * i, 0f);

            // Making order markers have correct numbers
            card.orderMarker.gameObject.SetActive(true);
            card.orderMarker.SetNumber(i + 1);
            card.unit.EnableOrderMarker(true, i + 1);

            // Applying slight random rotation to cards
            int randomAngle = Random.Range(-cardRotationIntensity, cardRotationIntensity);
            card.RotateCard(randomAngle);

            // Sorting the card in the rendering layer
            card.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Sorts the cards based on their X coordinate (smallest X first).
    /// </summary>
    public void SortCards(Player player)
    {
        player.cardsOnField.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
    }

    public void InitPlanCards(Player player)
    {
        foreach (Card card in player.cardsOnField)
        {
            card.transform.SetParent(GameManager.instance.planningManager.fieldHand.transform, false);
            card.OnHover(false);
        }
    }
}
