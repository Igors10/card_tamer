using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DrawingTool : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("refs")]
    [SerializeField] Image primaryColorImage;
    [SerializeField] Image secondaryColorImage;
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

        // getting drawing colors
        primaryColor = GameManager.instance.player.playerColor;
        primaryColorImage.color = primaryColor;

        secondaryColor = GameManager.instance.managerUI.workshop.chosenSecondColor;
        secondaryColorImage.color = secondaryColor;

        ToggleBrushColor(true);
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

    public void ToggleBrushColor(bool selectPrimary)
    {
        brushColor = (selectPrimary) ? primaryColor : secondaryColor;
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
        brushColor = (brushColor == GameManager.instance.player.playerColor) ? Color.white : GameManager.instance.player.playerColor;
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
    public Sprite[] GetSprite()
    {
        if (IsCanvasEmpty()) return new Sprite[0];

        Color[] sourcePixels = drawingTexture.GetPixels();
        int length = sourcePixels.Length;

        // Create pixel arrays for the two separate layers
        Color[] primaryPixels = new Color[length];
        Color[] secondaryPixels = new Color[length];

        for (int i = 0; i < length; i++)
        {
            Color p = sourcePixels[i];

            // Skip transparent pixels
            if (p.a < 0.1f)
            {
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = Color.clear;
                continue;
            }

            // Check if pixel belongs to Color 1 or Color 2 (with a small tolerance threshold)
            if (IsColorMatch(p, primaryColor))
            {
                // Whitened for primary layer
                primaryPixels[i] = new Color(1f, 1f, 1f, p.a);
                secondaryPixels[i] = Color.clear;
            }
            else if (IsColorMatch(p, secondaryColor))
            {
                // Whitened for secondary layer
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = new Color(1f, 1f, 1f, p.a);
            }
            else
            {
                // Fallback for blending/anti-aliased edges if any
                primaryPixels[i] = Color.clear;
                secondaryPixels[i] = Color.clear;
            }
        }

        // Create separate Texture2Ds for each layer
        Texture2D primaryTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        primaryTex.SetPixels(primaryPixels);
        primaryTex.Apply();

        Texture2D secondaryTex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        secondaryTex.SetPixels(secondaryPixels);
        secondaryTex.Apply();

        // Generate and return both sprites
        Sprite[] twoColorSprites = new Sprite[2];
        twoColorSprites[0] = Sprite.Create(primaryTex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));
        twoColorSprites[0] = Sprite.Create(secondaryTex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f));

        return twoColorSprites;
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
