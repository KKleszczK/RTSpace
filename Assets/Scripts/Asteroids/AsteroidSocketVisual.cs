using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public class AsteroidSocketVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AsteroidFieldVisual asteroidFieldVisual;

    [Header("Circle")]
    [SerializeField] private float radius = 3f;
    [SerializeField, Min(8)] private int segments = 64;
    [SerializeField] private float lineWidth = 0.08f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        Rebuild();
    }

    private void OnEnable()
    {
        Rebuild();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Rebuild();
    }
#endif

    public void Rebuild()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
            return;

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;

        if (asteroidFieldVisual != null)
        {
            lineRenderer.startColor = asteroidFieldVisual.borderColor;
            lineRenderer.endColor = asteroidFieldVisual.borderColor;
        }

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector3 point = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            lineRenderer.SetPosition(i, point);
        }
    }

    public void SetFieldVisual(AsteroidFieldVisual fieldVisual)
    {
        asteroidFieldVisual = fieldVisual;
        Rebuild();
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        Rebuild();
    }


}