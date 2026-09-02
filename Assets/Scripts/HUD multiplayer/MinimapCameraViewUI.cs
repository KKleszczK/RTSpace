using UnityEngine;

public class MinimapCameraViewUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RtsCameraController cameraController;

    [SerializeField]
    private MinimapUI minimap;

    [Header("Lines")]
    [SerializeField]
    private RectTransform bottomLine;

    [SerializeField]
    private RectTransform rightLine;

    [SerializeField]
    private RectTransform topLine;

    [SerializeField]
    private RectTransform leftLine;

    [Header("Visual")]
    [SerializeField]
    private float lineThickness = 2f;


    private void LateUpdate()
    {
        UpdateCameraView();
    }


    private void UpdateCameraView()
    {
        if (cameraController == null ||
            minimap == null)
        {
            return;
        }

        if (!cameraController.TryGetViewCornersOnMap(
                out Vector3[] corners))
        {
            return;
        }

        Vector2 bottomLeft =
            minimap.WorldToMinimapLocal(
                corners[0]);

        Vector2 bottomRight =
            minimap.WorldToMinimapLocal(
                corners[1]);

        Vector2 topRight =
            minimap.WorldToMinimapLocal(
                corners[2]);

        Vector2 topLeft =
            minimap.WorldToMinimapLocal(
                corners[3]);

        SetLine(
            bottomLine,
            bottomLeft,
            bottomRight);

        SetLine(
            rightLine,
            bottomRight,
            topRight);

        SetLine(
            topLine,
            topRight,
            topLeft);

        SetLine(
            leftLine,
            topLeft,
            bottomLeft);
    }


    private void SetLine(
        RectTransform line,
        Vector2 start,
        Vector2 end)
    {
        if (line == null)
            return;

        Vector2 direction =
            end - start;

        float distance =
            direction.magnitude;

        Vector2 middle =
            (start + end) * 0.5f;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x) *
            Mathf.Rad2Deg;

        line.anchoredPosition =
            middle;

        line.sizeDelta =
            new Vector2(
                distance,
                lineThickness);

        line.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle);
    }
}