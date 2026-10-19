using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Workshop : MonoBehaviour
{
    [Header("starting")]
    bool startingSequence;
    [SerializeField] TextMeshProUGUI startingMinionCounter;

    [Header("creating cards")]
    [SerializeField] GameObject cardCreationUI;
    [SerializeField] GameObject abilitySelectUI;
    [SerializeField] GameObject secondaryColorSelectUI;
    [SerializeField] GameObject canvasUI;
    [SerializeField] WorkshopOption[] abilityOption = new WorkshopOption[2];
    [SerializeField] TMP_InputField nameInput;
    [HideInInspector] public AbilityObj chosenAbility;
    [HideInInspector] public Color chosenAbilityColor;
    [HideInInspector] public Color chosenSecondColor;
    public DrawingTool drawingCanvas;
    [SerializeField] Sprite placeholderSprite;

    [Header("refs")]
    Player player;
    CardGenerator generator;
    [SerializeField] TextMeshProUGUI abilityReminder;
    [SerializeField] TextMeshProUGUI abilityReminderPower;
    [SerializeField] GameObject savePresetButton; // saving presets only works in the editor
    [SerializeField] GameObject randomPresetButton;
    [SerializeField] UnitPresetList galleryList;
    UnitPreset loadedGalleryPreset;
    [SerializeField] GameObject createButton;
    [SerializeField] AutoFade createdCardPresenter;
    [SerializeField] Card cardPresenterPreviewCard;
    [SerializeField] GameObject missingNameText;
    [SerializeField] GameObject workshopHintObj;
    [SerializeField] TextMeshProUGUI workshopHintText;
    [SerializeField] GameObject colorSelectUI;

    [Header("workshop animation")]
    [SerializeField] GameObject background;
    [SerializeField] GameObject[] bgItems;
    [SerializeField] Transform[] bgItemPositions;
    Vector3[] bgItemStartingPositions = new Vector3[3];
    Vector3[] bgItemWorkshopPositions = new Vector3[3];
    [SerializeField] float appearanceTime;

    [Header("color select")]
    [SerializeField] GameObject pencil;

    private void Start()
    {
        player = GameManager.instance.player;
        generator = GameManager.instance.cardGenerator;

        // passing workshop reference to card options
        for (int a = 0; a < abilityOption.Length; a++)
        {
            abilityOption[a].workshop = this;
        }

        // getting starting bg items positions
        for (int a = 0; a < bgItems.Length;  a++)
        {
            bgItemStartingPositions[a] = bgItems[a].transform.localPosition;
            bgItemWorkshopPositions[a] = bgItemPositions[a].transform.localPosition;
        }

        WorkshopColorSelect();
    }

    public IEnumerator WorkshopAnim(bool isAppearing, bool moveBackground = true)
    {
        // disabling workshop hint
        workshopHintObj.SetActive(isAppearing);

        Vector3[] startingPos = (isAppearing) ? bgItemStartingPositions : bgItemWorkshopPositions;
        Vector3[] targetPos = (isAppearing) ? bgItemWorkshopPositions : bgItemStartingPositions;
        Vector3 bgTargetPos = (isAppearing) ? Vector3.zero : new Vector3(0, -Camera.main.pixelHeight -200f, 0);
        Vector3 bgStartingPos = (isAppearing) ? new Vector3(0, -Camera.main.pixelHeight - 200f, 0) : Vector3.zero;

        float t = 0;

        while (t < appearanceTime)
        {
            t += Time.deltaTime;
            float clampedT = t / appearanceTime;
            float coolT = 1 - (1 - clampedT) * (1 - clampedT);

            // moving each object
            for (int i = 0; i < bgItems.Length; i++)
            {
                bgItems[i].transform.localPosition = Vector3.Lerp(startingPos[i], targetPos[i], coolT);
            }

            // moving background
            if (moveBackground) background.transform.localPosition = Vector3.Lerp(bgStartingPos, bgTargetPos, clampedT); 

            yield return null;
        }

        // snapping each object to correct position
        for (int i = 0; i < bgItems.Length; i++)
        {
            bgItems[i].transform.localPosition = targetPos[i];
        }

        // deactivating itself after playing the "fading away animation"
        if (isAppearing == false) gameObject.SetActive(false);
    }

    public void LaunchStartingSequence()
    {
        // getting secondary color (lighter version of the main color, used by the marker)
        chosenSecondColor = Colors.instance.GetLightenColor(player.playerColor);

        // enabling correct workshop visuals
        colorSelectUI.SetActive(false);
        GameManager.instance.managerUI.EnableWorkshop(true, false);

        // creating starting hand for the opponent
        if (GameManager.instance.opponent.isAI) GameManager.instance.opponent.AIplayer.CreateAIStartingHand();

        startingSequence = true;
        AbilityOptions();
    }

    public void ChangeHint(string newHintText)
    {
        workshopHintText.text = newHintText;
    }

    public void WorkshopColorSelect()
    {
        // Welcoming message from Rat Smart
        if (GameData.firstTimePlaying)
        {
            StartCoroutine(RatSmart.instance.PlayDialogue("WelcomeDial"));
        }

        colorSelectUI.SetActive(true);
        workshopHintText.text = "Pick a color you like!";
    }

    public void AbilityOptions(bool drawSpecial = false)
    {
        // disabling shop UI
        GameManager.instance.gameStateUI[(int)GameState.BUYING].SetActive(false);
        GameManager.instance.readyButton.gameObject.SetActive(false);

        // enabling card choice buttons
        cardCreationUI.SetActive(true);
        EnableAbilityOptions(true);

        // writing the correct hint text
        workshopHintText.text = "Choose an ability for a new card";

        
        if (drawSpecial) // options for special cards
        {
            for (int a = 0; a < abilityOption.Length; a++)
            {
                AbilityObj specialAbilityToAssign = generator.PickRandomAbility("special");
                abilityOption[a].abilityNote.InitAbilityNote(specialAbilityToAssign);
            }
        }
        else // options for basic cards
        {
            // first option is always a standart +1 power card
            abilityOption[0].abilityNote.InitAbilityNote(generator.PickRandomAbility("attack"));

            // second is a random card
            AbilityObj abilityToAssign = generator.PickRandomAbility("basic");
            abilityOption[1].abilityNote.InitAbilityNote(abilityToAssign);
        }

        // Rat explaining the abilites
        if (RatSmart.instance.abilitiesExplained == false && GameData.firstTimePlaying)
        {
            StartCoroutine(RatSmart.instance.PlayDialogue("AbilityExplainDial"));
            RatSmart.instance.abilitiesExplained = true;
        }
    }

    void EnableAbilityOptions(bool isEnable)
    {
        abilitySelectUI.SetActive(isEnable);
        canvasUI.SetActive(false);
    }

    void EnableDrawingCanvas()
    {
        // enalbing the drawing canvas gameObject
        canvasUI.SetActive(true);

        // writing the correct hint text
        workshopHintText.text = "Draw and name the card";

        // passing the ability
        abilityReminder.text = chosenAbility.GetAbilityDesc(chosenAbilityColor);
        abilityReminderPower.text = chosenAbility.power.ToString();

        // resetting name input field
        nameInput.text = "";

        savePresetButton.SetActive(Application.isEditor);

        // Rat explaining the drawing
        if (RatSmart.instance.drawingExplained == false && GameData.firstTimePlaying)
        {
            StartCoroutine(RatSmart.instance.PlayDialogue("DrawingExplainDial"));
            RatSmart.instance.drawingExplained = true;
        }
    }

    public void PickAbilityOption(AbilityObj pickedAbility, Color abilityColor)
    {
        // save the ability and color
        chosenAbility = pickedAbility;
        chosenAbilityColor = abilityColor;

        // play Audio Effect
        AudioManager.instance.PlaySFX("ShopStarSFX");

        // switching to second color UI
        workshopHintText.text = "Pick second color for the card";
        abilitySelectUI.SetActive(false);
        EnableDrawingCanvas();
    }

    public void PickSecondaryColor(Color pickedColor)
    {
        // saving the picked color
        chosenSecondColor = pickedColor;

        // switching to drawing UI
        secondaryColorSelectUI.SetActive(false);
        EnableDrawingCanvas();
    }

    void PresentNewCard(CreatureObj newCardData)
    {
        createdCardPresenter.gameObject.SetActive(true);
        cardPresenterPreviewCard.AssignCardData(newCardData, GameManager.instance.player);
    }

    private void Update()
    {
        WorkshopInput();
    }

    void WorkshopInput()
    {
        // stop presenting card
        if (createdCardPresenter.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Mouse0)) createdCardPresenter.gameObject.SetActive(false);

        // ctrl + z undo
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) drawingCanvas.UndoStroke();

        // enter developers mode (canvas for drawing sprites)
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.D)) drawingCanvas.DevelopersMode();
    }

    public void CreateNewCard()
    {
        // if in developers mode save the card
        if (drawingCanvas.developersMode)
        {
            SaveCardToProject(nameInput.text, drawingCanvas.GetTextures());

            // reset the canvas
            nameInput.text = "";
            drawingCanvas.ClearCanvas();

            return;
        }

        // creating the card and giving it to player
        CreatureObj newCardData;

        Sprite[] newCardSprites = new Sprite[2];
        Texture2D[] canvasTextures = drawingCanvas.GetTextures();
        for (int i = 0; i < newCardSprites.Length; i++)
        {
            if (drawingCanvas.IsCanvasEmpty() == false && canvasTextures[i] != null) newCardSprites[i] = Sprite.Create(canvasTextures[i], new Rect(0, 0, drawingCanvas.textureSize, drawingCanvas.textureSize), new Vector2(0.5f, 0.5f));
        }
      
        newCardData = newCardData = generator.ConstructNewCard(nameInput.text, newCardSprites, chosenAbility, chosenSecondColor, chosenAbilityColor);

        // if the canvas is empty applying a random unit sprite and name instead
        if (drawingCanvas.IsCanvasEmpty())
        {
            UnitPreset randomUnitPreset = GameManager.instance.cardDatabase.GetRandomBasicPreset();
            // presets are made with the darker second color in mind (same as AI cards)
            newCardData = generator.ConstructNewCard(randomUnitPreset.unitName, randomUnitPreset.sprite, chosenAbility, Colors.instance.GetDarkenColor(player.playerColor), chosenAbilityColor);
        }
        // also checking if the minion is named
        else if (nameInput.text == "")
        {
            // telling player to name the card
            missingNameText.SetActive(true);
            Animations.instance.ShakeAnim(nameInput.gameObject, 5f, 1f);

            return;
        }

        generator.CreateCard(newCardData, GameManager.instance.player);

        // Present new card to player
        PresentNewCard(newCardData);

        // if starting sequence continue until player has 5 cards
        if (startingSequence)
        {
            // updating starting minion counter
            startingMinionCounter.text = "cards " + player.cardsInHand.Count + "/" + GameManager.instance.startingCardAmount;

            // no more options if player has 5 cards
            if (player.cardsInHand.Count == GameManager.instance.startingCardAmount) { StopStartingSequence(); return; }

            // otherwise continue choosing (drawing) cards
            AbilityOptions();
        }
        else
        {
            // go back to shop if it is not a starting sequence
            cardCreationUI.SetActive(false);
            GameManager.instance.readyButton.gameObject.SetActive(true);
            GameManager.instance.readyButton.buttonText.text = "Ready";
            GameManager.instance.gameStateUI[(int)GameState.BUYING].SetActive(true);
        }
    }

    /// <summary>
    /// Save button: saves the current drawing as a unit preset (same as developers mode), without creating the card
    /// </summary>
    public void SaveCanvasAsPreset()
    {
        AudioManager.instance.PlaySFX("ButtonSFX");

        // nothing to save
        if (drawingCanvas.IsCanvasEmpty()) { Animations.instance.ShakeAnim(drawingCanvas.gameObject, 5f, 1f); return; }

        // presets need a name
        if (nameInput.text == "")
        {
            missingNameText.SetActive(true);
            Animations.instance.ShakeAnim(nameInput.gameObject, 5f, 1f);
            return;
        }

        UnitPreset savedPreset = SaveCardToProject(GetFreePresetName(nameInput.text), drawingCanvas.GetTextures(), galleryTextureFolderPath, galleryFolderPath, false);

#if UNITY_EDITOR
        if (savedPreset != null)
        {
            // file name can have a number added, the preset keeps the typed name
            savedPreset.unitName = nameInput.text;
            EditorUtility.SetDirty(savedPreset);
            AssetDatabase.SaveAssets();
        }
        SyncGalleryList();
#endif
    }

    // drawings saved with the save button (presets in the gallery folder, their layer sprites in a subfolder)
    const string galleryFolderPath = "Assets/ScrObjects/Gallery/";
    const string galleryTextureFolderPath = galleryFolderPath + "Textures/";

    /// <summary>
    /// Random preset button: puts a random gallery drawing (different from the current one) on the canvas and enters its name
    /// </summary>
    public void LoadRandomGalleryPreset()
    {
        AudioManager.instance.PlaySFX("ButtonSFX");

#if UNITY_EDITOR
        SyncGalleryList();
#endif

        List<UnitPreset> options = galleryList.presets.FindAll(preset => preset != null && preset != loadedGalleryPreset);
        // only one drawing in the gallery and it's already loaded
        if (options.Count == 0 && loadedGalleryPreset != null) options.Add(loadedGalleryPreset);
        if (options.Count == 0) { Animations.instance.ShakeAnim(randomPresetButton, 5f, 1f); return; }

        loadedGalleryPreset = options[Random.Range(0, options.Count)];
        drawingCanvas.LoadLayers(loadedGalleryPreset.sprite);

        nameInput.text = loadedGalleryPreset.unitName;
        missingNameText.SetActive(false);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Makes the gallery list match the presets in the gallery folder (the list is what builds use)
    /// </summary>
    void SyncGalleryList()
    {
        List<UnitPreset> folderPresets = new List<UnitPreset>();
        foreach (string guid in AssetDatabase.FindAssets("t:UnitPreset", new[] { galleryFolderPath.TrimEnd('/') }))
        {
            folderPresets.Add(AssetDatabase.LoadAssetAtPath<UnitPreset>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        galleryList.presets = folderPresets;
        EditorUtility.SetDirty(galleryList);
        AssetDatabase.SaveAssets();
    }
#endif

    /// <summary>
    /// Removes characters that can't be in a file name and adds a number if a preset with that name already exists
    /// </summary>
    string GetFreePresetName(string cardName)
    {
        foreach (char invalidChar in Path.GetInvalidFileNameChars()) cardName = cardName.Replace(invalidChar.ToString(), "");

        string freeName = cardName;
        int number = 2;
        while (File.Exists($"{galleryFolderPath}{freeName}.asset")) freeName = cardName + " " + number++;
        return freeName;
    }

    // Calling the function:
    // Texture2D[] splitTextures = drawingCanvas.GetTwoToneTextures();
    // if (splitTextures != null) SaveCardToProject(nameInput.text, splitTextures[0], splitTextures[1]);

    public UnitPreset SaveCardToProject(string cardName, Texture2D[] cardTextureArray,
        string spriteFolderPath = "Assets/Graphics/CardGraphics/UnitSprites/", string presetFolderPath = "Assets/ScrObjects/UnitPresets/", bool selectInProject = true)
    {
#if UNITY_EDITOR
        // 1. Define folder and file paths for both layers
        Directory.CreateDirectory(spriteFolderPath);
        Directory.CreateDirectory(presetFolderPath);

        string primaryPath = $"{spriteFolderPath}{cardName}_1.png";
        string secondaryPath = $"{spriteFolderPath}{cardName}_2.png";
        string presetFilePath = $"{presetFolderPath}{cardName}.asset";

        // 2. Encode and save BOTH textures as PNG files 
        File.WriteAllBytes(primaryPath, cardTextureArray[0].EncodeToPNG()); 
        File.WriteAllBytes(secondaryPath, cardTextureArray[1].EncodeToPNG()); 

        // 3. Force Unity to recognize the new PNG files
        AssetDatabase.Refresh(); 

        // 4. Set import settings for both primary and secondary PNGs
        ConfigureSpriteImporter(primaryPath);
        ConfigureSpriteImporter(secondaryPath);

        // Refresh database again to ensure Sprite sub-assets are generated
        AssetDatabase.Refresh();

        // 5. Load the newly imported Sprite assets
        Sprite[] presetSpritesArray = new Sprite[2];
        presetSpritesArray[0] = AssetDatabase.LoadAssetAtPath<Sprite>(primaryPath);
        presetSpritesArray[1] = AssetDatabase.LoadAssetAtPath<Sprite>(secondaryPath);

        // 6. Create a new instance of your ScriptableObject 
        UnitPreset newCardAsset = ScriptableObject.CreateInstance<UnitPreset>();

        // 7. Assign both layer sprites to the ScriptableObject 
        newCardAsset.unitName = cardName;
        newCardAsset.sprite = presetSpritesArray;   
        
        // 8. Create the actual .asset file in the project folder 
        AssetDatabase.CreateAsset(newCardAsset, presetFilePath);

        // 9. Save changes and focus in Inspector 
        AssetDatabase.SaveAssets();
        if (selectInProject)
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newCardAsset;
        }

        Debug.Log($"Successfully created dual-layer preset for: {cardName}");
        return newCardAsset;
#else
    Debug.LogWarning("Card saving is only supported inside the Unity Editor.");
    return null;
#endif
    }

    // Helper method to keep importer logic clean
    private void ConfigureSpriteImporter(string filePath)
    {
#if UNITY_EDITOR
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(filePath);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite; // [cite: 232]
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.isReadable = true; // needed to load the drawing back onto the canvas
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
#endif
    }


    void StopStartingSequence()
    {
        startingSequence = false;
        cardCreationUI.SetActive(false);
        GameManager.instance.readyButton.gameObject.SetActive(true);
        startingMinionCounter.gameObject.SetActive(false);
    }
}