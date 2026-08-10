using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DrawingTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("refs")]
    [SerializeField] PencilButton primaryColorImage;
    [SerializeField] PencilButton secondaryColorImage;
    Color primaryColor;
    Color secondaryColor;

    [Header("canvas attributes")]
    [SerializeField] Image canvasBG;
    public RawImage canvasImage;
    public int textureSize = 256;
    public int smallBrushSize = 2;
    public int normalBrushSize = 4;
    public int bigBrushSize = 7;
    int currentBrushSize;
    public Color brushColor = Color.black;

    private Vector2Int lastPixelPos;
    [HideInInspector] public bool isDrawing = false;

    [HideInInspector] public Texture2D drawingTexture;
    private Stack<Color[]> undoStack = new Stack<Color[]>();

    [HideInInspector] public bool developersMode = false;
    void OnEnable()
    {
        // Create a blank white texture
        drawingTexture = new Texture2D(textureSize, textureSize);
        ClearCanvas();

        // Assign the texture to the RawImage
        canvasImage.texture = drawingTexture;

        // setting default brush size
        currentBrushSize = smallBrushSize;

        // setting the colors
        SetCanvasColors(GameManager.instance.player.playerColor, GameManager.instance.managerUI.workshop.chosenSecondColor);
    }

    public void SelectBrush(string brushSize)
    {
        switch (brushSize)
        {
            case "small": currentBrushSize = smallBrushSize; break;
            case "normal": currentBrushSize = normalBrushSize; break;
            case "big": currentBrushSize = bigBrushSize; break;
        }
    }

    void SetCanvasColors(Color primaryCol, Color secondaryCol)
    {
        // getting drawing colors
        primaryColor = primaryCol;
        primaryColorImage.SetColor(primaryColor);

        secondaryColor = secondaryCol;
        secondaryColorImage.SetColor(secondaryColor);

        // selecting main color
        ToggleBrushColor(true);
    }

    public void ToggleBrushColor(bool selectPrimary)
    {
        brushColor = (selectPrimary) ? primaryColor : secondaryColor;

        primaryColorImage.SelectPencil(selectPrimary, false);
        //if (!selectPrimary) primaryColorImage.Highlight(false);

        secondaryColorImage.SelectPencil(!selectPrimary, false);
        //if (selectPrimary) secondaryColorImage.Highlight(false);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDrawing = false;
    }

    public bool IsCanvasEmpty()
    {
        // 1. Grab all the pixels from the texture at once
        Color[] pixels = drawingTexture.GetPixels();

        // 2. Loop through every single pixel
        for (int i = 0; i < pixels.Length; i++)
        {
            // 3. If even ONE pixel is NOT our transparent background color...
            if (pixels[i] != Color.clear)
            {
                return false; // The canvas has been drawn on! It is NOT empty.
            }
        }

        // 4. If the loop finishes, it means every single pixel was Color.clear
        return true; // The canvas IS empty!
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // Ignore middle clicks or any other weird buttons
        if (eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Right) return;

        // Save state for Undo
        undoStack.Push(drawingTexture.GetPixels());

        // Determine what color to use based on the mouse button
        Color activeColor = (eventData.button == PointerEventData.InputButton.Right) ? Color.clear : brushColor;
        currentBrushSize = (eventData.button == PointerEventData.InputButton.Right) ? bigBrushSize : smallBrushSize;

        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            lastPixelPos = currentPixel;
            PaintPixels(currentPixel.x, currentPixel.y, activeColor);
            isDrawing = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        // Determine what color to use based on the mouse button
        Color activeColor = (eventData.button == PointerEventData.InputButton.Right) ? Color.clear : brushColor;

        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            DrawLine(lastPixelPos, currentPixel, activeColor);
            lastPixelPos = currentPixel;
        }
    }

    // Helper function to turn a screen click into a Texture Pixel coordinate
    private bool TryGetPixelPosition(PointerEventData eventData, out Vector2Int pixelPos)
    {
        pixelPos = Vector2Int.zero;
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            float normalizedX = (localPoint.x - rectTransform.rect.x) / rectTransform.rect.width;
            float normalizedY = (localPoint.y - rectTransform.rect.y) / rectTransform.rect.height;

            pixelPos.x = Mathf.RoundToInt(normalizedX * textureSize);
            pixelPos.y = Mathf.RoundToInt(normalizedY * textureSize);
            return true;
        }
        return false;
    }

    public void UndoStroke()
    {
        if (undoStack.Count > 0)
        {
            // Pop the last saved pixel array and apply it
            Color[] previousState = undoStack.Pop();
            drawingTexture.SetPixels(previousState);
            drawingTexture.Apply();
        }
    }

    public void ClearCanvas()
    {
        Color[] emptyPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < emptyPixels.Length; i++) emptyPixels[i] = Color.clear;

        drawingTexture.SetPixels(emptyPixels);
        drawingTexture.Apply();
    }

    private void PaintPixels(int x, int y, Color colorToPaint)
    {
        int radiusSquared = currentBrushSize * currentBrushSize;

        for (int i = -currentBrushSize; i <= currentBrushSize; i++)
        {
            for (int j = -currentBrushSize; j <= currentBrushSize; j++)
            {
                if ((i * i) + (j * j) <= radiusSquared)
                {
                    int pX = x + i;
                    int pY = y + j;

                    if (pX >= 0 && pX < textureSize && pY >= 0 && pY < textureSize)
                    {
                        drawingTexture.SetPixel(pX, pY, colorToPaint);
                    }
                }
            }
        }
        drawingTexture.Apply();
    }

    private void DrawLine(Vector2Int start, Vector2Int end, Color colorToPaint)
    {
        float distance = Vector2Int.Distance(start, end);

        if (distance == 0)
        {
            PaintPixels(start.x, start.y, colorToPaint);
            return;
        }

        for (float i = 0; i <= distance; i++)
        {
            float t = i / distance;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

            PaintPixels(x, y, colorToPaint);
        }
    }

    public void DevelopersMode()
    {
        ClearCanvas();
        developersMode = !developersMode;
        canvasBG.color = (canvasBG.color == Color.black) ? Color.white : Color.black;

        if (developersMode) SetCanvasColors(Color.white, Color.gray);
        else SetCanvasColors(GameManager.instance.player.playerColor, GameManager.instance.managerUI.workshop.chosenSecondColor);
    }

    // === Getting the sprite ===
    // ==========================
    /// <summary>
    /// Converting the whole sprite to white, the color will later be reaplied during card rendering.
    /// </summary>
    /// <returns></returns>
    public void MakeSpriteWhite()
    {
        // no need to do it if the canvas is empty
        if (IsCanvasEmpty()) return;

        // getting the pixels and "whitening" them
        Color[] pixels = drawingTexture.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(1f, 1f, 1f, pixels[i].a);
        }

        drawingTexture.SetPixels(pixels);
        drawingTexture.Apply();
    }

    /// <summary>
    /// Returns two sprites for each color
    /// </summary>
    /// <param name="primaryColor"></param>
    /// <param name="secondaryColor"></param>
    /// <returns></returns>
    public Texture2D[] GetTextures()
    {
        if (IsCanvasEmpty()) return null;

        Color[] sourcePixels = drawingTexture.GetPixels(); // [cite: 37]
        int length = sourcePixels.Length;

        Color[] primaryPixels = new Color[length];
        Color[] secondaryPixels = new Color[length];

        for (int i = 0; i < length; i++)
        {
            Color p = sourcePixels[i];

            if (p.a < 0.1f)
            {
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = Color.clear;
                continue;
            }

            if (IsColorMatch(p, primaryColor))
            {
                primaryPixels[i] = new Color(1f, 1f, 1f, p.a); // [cite: 38]
                secondaryPixels[i] = Color.clear;
            }
            else if (IsColorMatch(p, secondaryColor))
            {
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = new Color(1f, 1f, 1f, p.a); // [cite: 38]
            }
            else
            {
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = Color.clear;
            }
        }

        Texture2D primaryTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        primaryTex.SetPixels(primaryPixels);
        primaryTex.Apply(); // [cite: 39]

        Texture2D secondaryTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        secondaryTex.SetPixels(secondaryPixels);
        secondaryTex.Apply(); // [cite: 39]

        return new Texture2D[] { primaryTex, secondaryTex };
    }

    // Helper to account for tiny floating-point variations when comparing colors
    private bool IsColorMatch(Color c1, Color c2, float threshold = 0.1f)
    {
        return Mathf.Abs(c1.r - c2.r) < threshold &&
               Mathf.Abs(c1.g - c2.g) < threshold &&
               Mathf.Abs(c1.b - c2.b) < threshold;
    }

    // ================================
}
