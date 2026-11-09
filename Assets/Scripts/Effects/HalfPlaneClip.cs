using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cuts away the part of a UI graphic that is on the wrong side of a line by changing its mesh,
/// so it works with any shader (masks don't work with the unit outline shader). Used for cutting cards in half
/// </summary>
public class HalfPlaneClip : BaseMeshEffect
{
    Transform space; // the line is defined in this transform's local space
    Vector2 linePoint;
    Vector2 lineNormal; // the side the normal points to is kept

    readonly List<UIVertex> stream = new List<UIVertex>();
    readonly List<UIVertex> output = new List<UIVertex>();
    readonly List<UIVertex> triangle = new List<UIVertex>(3);
    readonly List<UIVertex> clipped = new List<UIVertex>(4);

    public void SetLine(Transform newSpace, Vector2 point, Vector2 normal)
    {
        space = newSpace;
        linePoint = point;
        lineNormal = normal.normalized;

        // text doesn't use mesh effects, its letters get hidden whole on the cut away side instead
        if (graphic is TMP_Text text)
        {
            text.OnPreRenderText -= HideCutLetters;
            text.OnPreRenderText += HideCutLetters;

            // inactive text has no mesh yet, it gets cut when it builds one
            if (!text.isActiveAndEnabled) return;
            text.ForceMeshUpdate();
            HideCutLetters(text.textInfo);
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
        else if (graphic != null) graphic.SetVerticesDirty();
    }

    protected override void OnDestroy()
    {
        if (graphic is TMP_Text text) text.OnPreRenderText -= HideCutLetters;
        base.OnDestroy();
    }

    void HideCutLetters(TMP_TextInfo textInfo)
    {
        if (space == null) return;
        Matrix4x4 toSpace = space.worldToLocalMatrix * transform.localToWorldMatrix;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo letter = textInfo.characterInfo[i];
            if (!letter.isVisible) continue;

            Vector2 center = toSpace.MultiplyPoint3x4((letter.bottomLeft + letter.topRight) / 2f);
            if (Vector2.Dot(center - linePoint, lineNormal) >= 0) continue;

            Color32[] colors = textInfo.meshInfo[letter.materialReferenceIndex].colors32;
            for (int k = 0; k < 4; k++) colors[letter.vertexIndex + k].a = 0;
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || space == null) return;

        // the graphic is a child of the space, so this matrix stays the same while the halves fly around
        Matrix4x4 toSpace = space.worldToLocalMatrix * transform.localToWorldMatrix;

        stream.Clear();
        output.Clear();
        vh.GetUIVertexStream(stream);

        // clipping every triangle against the line and turning what is left back into triangles
        for (int i = 0; i + 2 < stream.Count; i += 3)
        {
            triangle.Clear();
            triangle.Add(stream[i]);
            triangle.Add(stream[i + 1]);
            triangle.Add(stream[i + 2]);
            ClipPolygon(triangle, toSpace);

            for (int k = 1; k + 1 < clipped.Count; k++)
            {
                output.Add(clipped[0]);
                output.Add(clipped[k]);
                output.Add(clipped[k + 1]);
            }
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(output);
    }

    float SideOfLine(UIVertex vertex, Matrix4x4 toSpace)
    {
        Vector2 point = toSpace.MultiplyPoint3x4(vertex.position);
        return Vector2.Dot(point - linePoint, lineNormal);
    }

    void ClipPolygon(List<UIVertex> polygon, Matrix4x4 toSpace)
    {
        clipped.Clear();
        for (int i = 0; i < polygon.Count; i++)
        {
            UIVertex a = polygon[i];
            UIVertex b = polygon[(i + 1) % polygon.Count];
            float sideA = SideOfLine(a, toSpace);
            float sideB = SideOfLine(b, toSpace);

            if (sideA >= 0) clipped.Add(a);
            if ((sideA >= 0) != (sideB >= 0)) clipped.Add(LerpVertex(a, b, sideA / (sideA - sideB)));
        }
    }

    static UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
    {
        UIVertex vertex = a;
        vertex.position = Vector3.Lerp(a.position, b.position, t);
        vertex.normal = Vector3.Lerp(a.normal, b.normal, t);
        vertex.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
        vertex.color = Color32.Lerp(a.color, b.color, t);
        vertex.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
        vertex.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
        vertex.uv2 = Vector4.Lerp(a.uv2, b.uv2, t);
        vertex.uv3 = Vector4.Lerp(a.uv3, b.uv3, t);
        return vertex;
    }
}
