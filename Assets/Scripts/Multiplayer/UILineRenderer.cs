using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : Graphic
{
    [SerializeField] private float lineWidth = 3f;

    private readonly List<Vector2> points = new();


    public void SetPoints(List<Vector2> newPoints)
    {
        points.Clear();

        if (newPoints != null)
            points.AddRange(newPoints);

        SetVerticesDirty();
    }


    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++)
        {
            AddLineSegment(
                vh,
                points[i],
                points[i + 1]);
        }
    }


    private void AddLineSegment(
        VertexHelper vh,
        Vector2 start,
        Vector2 end)
    {
        Vector2 direction =
            (end - start).normalized;

        Vector2 normal =
            new Vector2(
                -direction.y,
                direction.x) *
            (lineWidth * 0.5f);

        int index = vh.currentVertCount;

        UIVertex vertex =
            UIVertex.simpleVert;

        vertex.color = color;

        vertex.position = start - normal;
        vh.AddVert(vertex);

        vertex.position = start + normal;
        vh.AddVert(vertex);

        vertex.position = end + normal;
        vh.AddVert(vertex);

        vertex.position = end - normal;
        vh.AddVert(vertex);

        vh.AddTriangle(
            index,
            index + 1,
            index + 2);

        vh.AddTriangle(
            index,
            index + 2,
            index + 3);
    }
}