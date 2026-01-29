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
            card.transform.position = firstCardPosition.position;
            card.transform.position += new Vector3(defaultCardInterval * i, 0f);

            // Making order markers have correct numbers
            card.orderMarker.gameObject.SetActive(true);
            card.orderMarker.SetNumber(i + 1);
            card.unit.EnableOrderMarker(true, i + 1);

            // Applying slight random rotation to cards
            int randomAngle = Random.Range(-cardRotationIntensity, cardRotationIntensity);
            card.RotateCard(randomAngle);
        }
    }

    /// <summary>
    /// Sorts the cards based on their X coordinate (smallest X first).
    /// </summary>
    public void SortCards(Player player)
    {
        player.cardsOnField.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
    }
}
