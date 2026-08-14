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

        if (points == null ||
            points.Count < 3)
        {
            meshFilter.sharedMesh = null;
            return;
        }

        List<Vector3> vertices =
            new List<Vector3>();

        foreach (Transform point in points)
        {
            if (point == null)
                continue;

            Vector3 localPosition =
                transform.InverseTransformPoint(
                    point.position);

            localPosition.y =
                visualHeight - 0.01f;

            vertices.Add(
                localPosition);
        }

        if (vertices.Count < 3)
        {
            meshFilter.sharedMesh = null;
            return;
        }

        List<int> triangles =
            TriangulatePolygon(vertices);

        if (triangles.Count < 3)
        {
            Debug.LogWarning(
                "[ASTEROID FIELD] Nie uda³o siê triangulowaæ polygonu.",
                this);

            meshFilter.sharedMesh = null;
            return;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(
            triangles,
            0);

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh =
            mesh;

        fillMaterial.color =
            fillColor;
    }

    private List<int> TriangulatePolygon(
    List<Vector3> vertices)
    {
        List<int> result =
            new List<int>();

        if (vertices == null ||
            vertices.Count < 3)
        {
            return result;
        }

        List<int> indices =
            new List<int>();

        for (int i = 0;
             i < vertices.Count;
             i++)
        {
            indices.Add(i);
        }

        bool isClockwise =
            SignedArea(vertices) < 0f;

        int safetyCounter = 0;

        while (indices.Count > 3)
        {
            bool earFound = false;

            for (int i = 0;
                 i < indices.Count;
                 i++)
            {
                int previousIndex =
                    indices[
                        (i - 1 + indices.Count) %
                        indices.Count];

                int currentIndex =
                    indices[i];

                int nextIndex =
                    indices[
                        (i + 1) %
                        indices.Count];

                Vector3 a =
                    vertices[previousIndex];

                Vector3 b =
                    vertices[currentIndex];

                Vector3 c =
                    vertices[nextIndex];

                if (!IsConvex(
                        a,
                        b,
                        c,
                        isClockwise))
                {
                    continue;
                }

                bool containsPoint = false;

                for (int j = 0;
                     j < indices.Count;
                     j++)
                {
                    int testIndex =
                        indices[j];

                    if (testIndex == previousIndex ||
                        testIndex == currentIndex ||
                        testIndex == nextIndex)
                    {
                        continue;
                    }

                    Vector3 p =
                        vertices[testIndex];

                    if (PointInsideTriangleXZ(
                            p,
                            a,
                            b,
                            c))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                    continue;

                if (isClockwise)
                {
                    result.Add(previousIndex);
                    result.Add(currentIndex);
                    result.Add(nextIndex);
                }
                else
                {
                    result.Add(nextIndex);
                    result.Add(currentIndex);
                    result.Add(previousIndex);
                }

                indices.RemoveAt(i);

                earFound = true;
                break;
            }

            safetyCounter++;

            if (!earFound ||
                safetyCounter > 10000)
            {
                Debug.LogWarning(
                    "[ASTEROID FIELD] Ear clipping zatrzyma³ siê. " +
                    "SprawdŸ kolejnoœæ punktów i czy krawêdzie polygonu siê nie przecinaj¹.",
                    this);

                return new List<int>();
            }
        }

        if (indices.Count == 3)
        {
            if (isClockwise)
            {
                result.Add(indices[0]);
                result.Add(indices[1]);
                result.Add(indices[2]);
            }
            else
            {
                result.Add(indices[2]);
                result.Add(indices[1]);
                result.Add(indices[0]);
            }
        }

        return result;
    }

    private float SignedArea(
    List<Vector3> vertices)
    {
        float area = 0f;

        for (int i = 0;
             i < vertices.Count;
             i++)
        {
            Vector3 a =
                vertices[i];

            Vector3 b =
                vertices[
                    (i + 1) %
                    vertices.Count];

            area +=
                a.x * b.z -
                b.x * a.z;
        }

        return area * 0.5f;
    }

    private bool IsConvex(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        bool isClockwise)
    {
        float cross =
            (b.x - a.x) *
            (c.z - b.z)
            -
            (b.z - a.z) *
            (c.x - b.x);

        return isClockwise
            ? cross < 0f
            : cross > 0f;
    }

    private bool PointInsideTriangleXZ(
        Vector3 p,
        Vector3 a,
        Vector3 b,
        Vector3 c)
    {
        float d1 =
            SignXZ(
                p,
                a,
                b);

        float d2 =
            SignXZ(
                p,
                b,
                c);

        float d3 =
            SignXZ(
                p,
                c,
                a);

        bool hasNegative =
            d1 < 0f ||
            d2 < 0f ||
            d3 < 0f;

        bool hasPositive =
            d1 > 0f ||
            d2 > 0f ||
            d3 > 0f;

        return !(hasNegative &&
                 hasPositive);
    }

    private float SignXZ(
        Vector3 p1,
        Vector3 p2,
        Vector3 p3)
    {
        return
            (p1.x - p3.x) *
            (p2.z - p3.z)
            -
            (p2.x - p3.x) *
            (p1.z - p3.z);
    }

    public bool ContainsWorldPosition(
    Vector3 worldPosition)
    {
        if (points == null ||
            points.Count < 3)
        {
            return false;
        }

        /*
         * Point-in-polygon.
         *
         * Pracujemy na p³aszczyŸnie XZ,
         * poniewa¿ Y nie ma znaczenia
         * dla pola asteroid.
         */

        float x = worldPosition.x;
        float z = worldPosition.z;

        bool inside = false;

        int j =
            points.Count - 1;

        for (int i = 0;
             i < points.Count;
             i++)
        {
            Transform pointI =
                points[i];

            Transform pointJ =
                points[j];

            if (pointI == null ||
                pointJ == null)
            {
                j = i;
                continue;
            }

            float xi =
                pointI.position.x;

            float zi =
                pointI.position.z;

            float xj =
                pointJ.position.x;

            float zj =
                pointJ.position.z;

            bool intersects =
                ((zi > z) != (zj > z)) &&
                (x <
                    (xj - xi) *
                    (z - zi) /
                    (zj - zi) +
                    xi);

            if (intersects)
            {
                inside =
                    !inside;
            }

            j = i;
        }

        return inside;
    }
}