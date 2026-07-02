using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopColorSelect : MonoBehaviour
{
    [Header("pencils")]
    [SerializeField] GameObject firstPencil;
    [SerializeField] float pencilDistance;
    [SerializeField] List<PencilButton> pencils;
    [SerializeField] GameObject pencilObj;

    [Header("refs")]
    public GameObject selectButton;
    [HideInInspector] public PencilButton selectedPencil;

    private void Start()
    {
        InitPencils();
    }

    void InitPencils()
    {
        int pencilAmount = GameManager.instance.cardDatabase.allPlayerColors.Count;
        //int pencilAmount = 10;

        for (int i = 1; i < pencilAmount; i++)
        {
            Vector3 newPencilPos = new Vector3(firstPencil.transform.position.x + pencilDistance * i, firstPencil.transform.position.y, firstPencil.transform.position.z); 
            GameObject newPencil = Instantiate(firstPencil, newPencilPos, firstPencil.transform.rotation, pencilObj.transform);

            // Setting up the button
            PencilButton newPencilButton = newPencil.GetComponentInChildren<PencilButton>();
            pencils.Add(newPencilButton);
            newPencilButton.workshopColorSelect = this;
        }

        // Assigning colors
        for (int i = 0; i < pencilAmount; i++)
        {
            pencils[i].SetColor(GameManager.instance.cardDatabase.allPlayerColors[i]);

            // prechoosing the pencil if the player had that color selected
            //if (GameManager.instance.cardDatabase.allPlayerColors[i] == GameManager.instance.player.playerColor) ChoosePencil(pencils[i]);
        }


    }

    /// <summary>
    /// Triggers when a pencil is getting hovered over
    /// </summary>
    /// <param name="pencil"></param>
    public void HighlightPencil(GameObject pencil)
    {

    }

    public void ChoosePencil(PencilButton chosenPencil)
    {
        // nothing happens if selecting same pencil
        if (selectedPencil != null && selectedPencil == chosenPencil) return;

        // deselected previous pencil
        if (selectedPencil != null) selectedPencil.SelectPencil(false);

        // selecting new pencil
        chosenPencil.SelectPencil(true);

        // moves the other pencils aside
        //OffsetPencils();

        // enabling select button
        selectButton.SetActive(true);
    }

    /// <summary>
    /// Highlights the selected pencil by moving the rest away a bit (wip)
    /// </summary>
    void OffsetPencils()
    {
        bool offsetToLeft = true;
        for (int i = 0; i < pencils.Count; i++)
        {
            // do not move the selected pencil and tell the rest of the pencils to offset in a different direction from now on
            if (pencils[i] == selectedPencil)
            {
                pencils[i].OffsetPencil(0);
                offsetToLeft = false;
                continue;
            }

            // calculate offset
            float newOffsetX = (offsetToLeft) ? -pencilDistance : pencilDistance;

            pencils[i].OffsetPencil(newOffsetX);
        }
    }

    public void LockPencilChoice()
    {
        // playing SFX
        AudioManager.instance.PlaySFX("ButtonSFX");

        // locking the color
        GameManager.instance.player.playerColor = selectedPencil.pencilColorImage.color;
        GameManager.instance.player.config.playerColor = selectedPencil.pencilColorImage.color;

        // start creating player hand
        GameManager.instance.managerUI.workshop.LaunchStartingSequence();
    }
}
