using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DrawTool
{
    Pencil,     // primary color, thin, always on top
    Marker,     // secondary color, thicker, always under the pencil
    Eraser
}

public class DrawingTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("refs")]
    [SerializeField] PencilButton primaryColorImage;
    [SerializeField] PencilButton secondaryColorImage; // marker
    [SerializeField] PencilButton eraserButton;
    Color primaryColor;
    Color secondaryColor;
    DrawTool currentTool = DrawTool.Pencil;

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
        SelectTool(DrawTool.Pencil);
    }

    // button callbacks (enums can't be passed from the inspector)
    public void SelectPencil() => SelectTool(DrawTool.Pencil);
    public void SelectMarker() => SelectTool(DrawTool.Marker);
    public void SelectEraser() => SelectTool(DrawTool.Eraser);

    void SelectTool(DrawTool tool)
    {
        currentTool = tool;
        brushColor = (tool == DrawTool.Marker) ? secondaryColor : primaryColor;

        primaryColorImage.SelectPencil(tool == DrawTool.Pencil, false);
        secondaryColorImage.SelectPencil(tool == DrawTool.Marker, false);
        eraserButton.SelectPencil(tool == DrawTool.Eraser, false);
    }

    int GetToolBrushSize(DrawTool tool)
    {
        switch (tool)
        {
            case DrawTool.Marker: return normalBrushSize;
            case DrawTool.Eraser: return bigBrushSize;
            default: return smallBrushSize;
        }
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

        // right click always erases, left click uses the selected tool
        DrawTool activeTool = GetActiveTool(eventData);
        currentBrushSize = GetToolBrushSize(activeTool);

        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            lastPixelPos = currentPixel;
            PaintPixels(currentPixel.x, currentPixel.y, GetToolColor(activeTool), activeTool == DrawTool.Marker);
            isDrawing = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        DrawTool activeTool = GetActiveTool(eventData);

        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            DrawLine(lastPixelPos, currentPixel, GetToolColor(activeTool), activeTool == DrawTool.Marker);
            lastPixelPos = currentPixel;
        }
    }

    DrawTool GetActiveTool(PointerEventData eventData) => (eventData.button == PointerEventData.InputButton.Right) ? DrawTool.Eraser : currentTool;

    Color GetToolColor(DrawTool tool) => (tool == DrawTool.Eraser) ? Color.clear : brushColor;

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

    /// <summary>
    /// Replaces the drawing with two layer sprites (like the ones GetTextures makes), colored with the pencil and marker colors.
    /// The textures have to be readable
    /// </summary>
    public void LoadLayers(Sprite[] layers)
    {
        // so the loaded drawing can be undone
        undoStack.Push(drawingTexture.GetPixels());

        Color[] pixels = new Color[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // pencil layer goes over the marker layer
                Color pixel = Color.clear;
                if (GetLayerAlpha(layers, 1, x, y) >= 0.1f) pixel = secondaryColor;
                if (GetLayerAlpha(layers, 0, x, y) >= 0.1f) pixel = primaryColor;
                pixels[y * textureSize + x] = pixel;
            }
        }

        drawingTexture.SetPixels(pixels);
        drawingTexture.Apply();
    }

    float GetLayerAlpha(Sprite[] layers, int layer, int x, int y)
    {
        if (layers == null || layer >= layers.Length || layers[layer] == null) return 0f;

        // layer textures can have a different size than the canvas
        Texture2D layerTexture = layers[layer].texture;
        return layerTexture.GetPixel(x * layerTexture.width / textureSize, y * layerTexture.height / textureSize).a;
    }

    public void ClearCanvas()
    {
        Color[] emptyPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < emptyPixels.Length; i++) emptyPixels[i] = Color.clear;

        drawingTexture.SetPixels(emptyPixels);
        drawingTexture.Apply();
    }

    /// <summary>
    /// Paints a brush dot. With keepPencilOnTop pixels already drawn with the pencil (primary color) are left untouched
    /// </summary>
    private void PaintPixels(int x, int y, Color colorToPaint, bool keepPencilOnTop = false)
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
                        if (keepPencilOnTop && IsPencilPixel(drawingTexture.GetPixel(pX, pY))) continue;
                        drawingTexture.SetPixel(pX, pY, colorToPaint);
                    }
                }
            }
        }
        drawingTexture.Apply();
    }

    bool IsPencilPixel(Color pixel) => pixel.a >= 0.1f && IsColorMatch(pixel, primaryColor);

    private void DrawLine(Vector2Int start, Vector2Int end, Color colorToPaint, bool keepPencilOnTop = false)
    {
        float distance = Vector2Int.Distance(start, end);

        if (distance == 0)
        {
            PaintPixels(start.x, start.y, colorToPaint, keepPencilOnTop);
            return;
        }

        for (float i = 0; i <= distance; i++)
        {
            float t = i / distance;
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

            PaintPixels(x, y, colorToPaint, keepPencilOnTop);
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
