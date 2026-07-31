using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public class AsteroidFieldVisual : MonoBehaviour
{
    [Header("Polygon Points")]
    [SerializeField] private List<Transform> points = new();

    [Header("Visuals")]
    [SerializeField] public Color borderColor = Color.cyan;
    [SerializeField]
    private Color fillColor =
        new Color(0f, 0.5f, 1f, 0.15f);

    [SerializeField] private float borderWidth = 0.15f;
    [SerializeField] private float visualHeight = 0.05f;

    private LineRenderer border;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Material borderMaterial;
    private Material fillMaterial;
    private Mesh mesh;

    private void OnEnable()
    {
        CreateComponents();
        Rebuild();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Rebuild();
#endif
    }

    private void OnValidate()
    {
        borderWidth = Mathf.Max(0.01f, borderWidth);

        CreateComponents();
        Rebuild();
    }

    private void CreateComponents()
    {
        border = GetComponent<LineRenderer>();

        if (border == null)
            border = gameObject.AddComponent<LineRenderer>();

        meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        if (borderMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            borderMaterial = new Material(shader)
            {
                name = "AsteroidField_Border_Runtime"
            };
        }

        if (fillMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");

            fillMaterial = new Material(shader)
            {
                name = "AsteroidField_Fill_Runtime"
            };

            fillMaterial.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha);

            fillMaterial.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha);

            fillMaterial.SetInt("_ZWrite", 0);
            fillMaterial.renderQueue = 3000;
        }

        border.sharedMaterial = borderMaterial;
        meshRenderer.sharedMaterial = fillMaterial;
    }

    public void Rebuild()
    {
        if (points == null || points.Count < 3)
        {
            if (border != null)
                border.positionCount = 0;

            if (meshFilter != null)
                meshFilter.sharedMesh = null;

            return;
        }

        BuildBorder();
        BuildFill();
    }

    private void BuildBorder()
    {
        border.useWorldSpace = false;
        border.loop = true;
        border.positionCount = points.Count;
        border.startWidth = borderWidth;
        border.endWidth = borderWidth;
        border.startColor = borderColor;
        border.endColor = borderColor;

        borderMaterial.color = borderColor;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                continue;

            Vector3 localPosition =
                transform.InverseTransformPoint(points[i].position);

            localPosition.y = visualHeight;

            border.SetPosition(i, localPosition);
        }
    }

    private void BuildFill()
    {
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "AsteroidFieldPolygon"
            };
        }
        else
        {
            mesh.Clear();
        }

        Vector3[] vertices = new Vector3[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                continue;

            vertices[i] =
                transform.InverseTransformPoint(points[i].position);

            vertices[i].y = visualHeight - 0.01f;
        }

        // Prosty polygon wypuk³y:
        // punkt 0 ³¹czony jest z kolejnymi punktami.
        int[] triangles =
            new int[(points.Count - 2) * 3];

        int triangleIndex = 0;

        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = i;
            triangles[triangleIndex++] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        fillMaterial.color = fillColor;
    }
}