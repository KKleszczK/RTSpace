using System.Collections.Generic;
using UnityEngine;

public class ShipCommandPathVisual : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private LineRenderer segmentPrefab;

    [Header("Path")]
    [SerializeField]
    private float pathHeight = 0.05f;

    private ShipUnit ship;

    private readonly List<LineRenderer> segments =
        new();

    private bool visible;


    private void Awake()
    {
        ship =
            GetComponent<ShipUnit>();
    }


    private void LateUpdate()
    {
        if (!visible)
            return;

        RefreshPath();
    }


    public void SetVisible(
        bool value)
    {
        visible =
            value;

        if (!visible)
        {
            ClearSegments();
            return;
        }

        RefreshPath();
    }


    private void RefreshPath()
    {
        if (ship == null)
            return;

        IReadOnlyList<ShipUnit.VisualShipCommand> commands =
            ship.VisualCommands;

        EnsureSegmentCount(
            commands.Count);

        Vector3 startPosition =
            ship.transform.position;

        startPosition.y =
            pathHeight;

        for (int i = 0;
             i < commands.Count;
             i++)
        {
            ShipUnit.VisualShipCommand command =
                commands[i];

            Vector3 endPosition =
                command.Position;

            endPosition.y =
                pathHeight;

            LineRenderer line =
                segments[i];

            line.gameObject.SetActive(true);

            line.positionCount = 2;

            line.SetPosition(
                0,
                startPosition);

            line.SetPosition(
                1,
                endPosition);

            Color color =
                ShipCommandVisualSettings.Instance != null
                    ? ShipCommandVisualSettings.Instance.GetColor(
                        command.Type)
                    : Color.white;

            line.startColor =
                color;

            line.endColor =
                color;

            startPosition =
                endPosition;
        }
    }


    private void EnsureSegmentCount(
        int requiredCount)
    {
        while (segments.Count <
               requiredCount)
        {
            LineRenderer line =
                Instantiate(
                    segmentPrefab,
                    transform);

            segments.Add(
                line);
        }

        for (int i = 0;
             i < segments.Count;
             i++)
        {
            segments[i]
                .gameObject
                .SetActive(
                    i < requiredCount);
        }
    }


    private void ClearSegments()
    {
        foreach (LineRenderer line in segments)
        {
            if (line != null)
                line.gameObject.SetActive(false);
        }
    }
}