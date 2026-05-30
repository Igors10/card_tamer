using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class DrawingTool : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RawImage canvasImage;
    public int textureSize = 256;
    public int smallBrushSize = 2;
    public int normalBrushSize = 4;
    public int bigBrushSize = 7;
    int currentBrushSize;
    public Color brushColor = Color.black;

    private Vector2Int lastPixelPos;
    private bool isDrawing = false;

    private Texture2D drawingTexture;
    private Stack<Color[]> undoStack = new Stack<Color[]>();

    void Start()
    {
        // Create a blank white texture
        drawingTexture = new Texture2D(textureSize, textureSize);
        Color[] initialPixels = new Color[textureSize * textureSize];
        for (int i = 0; i < initialPixels.Length; i++) initialPixels[i] = Color.clear;

        drawingTexture.SetPixels(initialPixels);
        drawingTexture.Apply();

        // Assign the texture to the RawImage
        canvasImage.texture = drawingTexture;

        // setting default brush size
        currentBrushSize = smallBrushSize;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        // Save state for Undo
        undoStack.Push(drawingTexture.GetPixels());

        // Get the starting pixel and mark it as our last known position
        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            lastPixelPos = currentPixel;
            PaintPixels(currentPixel.x, currentPixel.y);
            isDrawing = true;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing) return;

        if (TryGetPixelPosition(eventData, out Vector2Int currentPixel))
        {
            // Draw a line connecting the previous frame's position to the current one
            DrawLine(lastPixelPos, currentPixel);

            // Update the last known position for the next frame
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

    // Fills in the gaps between the last mouse position and the current one
    private void DrawLine(Vector2Int start, Vector2Int end)
    {
        float distance = Vector2Int.Distance(start, end);

        if (distance == 0)
        {
            PaintPixels(start.x, start.y);
            return;
        }

        // Interpolate along the line based on the distance
        for (float i = 0; i <= distance; i++)
        {
            float t = i / distance;

            // Lerp calculates the points in between the start and end
            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));

            PaintPixels(x, y);
        }
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

    private void PaintPixels(int x, int y)
    {
        int radiusSquared = currentBrushSize * currentBrushSize;

        // Simple brush
        for (int i = -currentBrushSize; i <= currentBrushSize; i++)
        {
            for (int j = -currentBrushSize; j <= currentBrushSize; j++)
            {
                // Check if the current pixel coordinate falls inside the circle
                if ((i * i) + (j * j) <= radiusSquared)
                {
                    int pX = x + i;
                    int pY = y + j;

                    // Ensure we don't draw outside the texture bounds
                    if (pX >= 0 && pX < textureSize && pY >= 0 && pY < textureSize)
                    {
                        drawingTexture.SetPixel(pX, pY, brushColor);
                    }
                }
            }
        }
        // Apply the pixel changes to the texture
        drawingTexture.Apply();
    }
}
