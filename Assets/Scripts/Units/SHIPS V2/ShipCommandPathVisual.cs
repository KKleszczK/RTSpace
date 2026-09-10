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

    [SerializeField]
    private Transform attackMoveProgressMarker;


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
        RefreshAttackMoveProgressMarker();
    }


    public void SetVisible(
        bool value)
    {
        visible =
            value;

        if (!visible)
        {
            ClearSegments();

            if (attackMoveProgressMarker != null)
                attackMoveProgressMarker.gameObject.SetActive(false);

            return;
        }

        RefreshPath();
    }


    private void RefreshPath()
    {
        if (ship == null)
            return;

        ship.RefreshVisualCommandTargets();

        IReadOnlyList<ShipUnit.VisualShipCommand> commands =
            ship.VisualCommands;

        IReadOnlyList<Vector3> guardPoints =
            ship.VisualGuardPoints;

        


        // =========================================================
        // LICZYMY POTRZEBNE SEGMENTY
        // =========================================================

        int requiredSegments = 0;

        for (int i = 0; i < commands.Count; i++)
        {
            ShipUnit.VisualShipCommand command =
                commands[i];

            if (command.Type ==
                ShipUnit.ShipCommandType.Guard &&
                    guardPoints.Count > 0)
            {
                bool guardAlreadyStarted =
                    guardPoints.Count > 1 &&
                    ship.CurrentState !=
                        ShipUnit.ShipState.Passive;

                requiredSegments +=
                    guardAlreadyStarted
                        ? guardPoints.Count - 1
                        : guardPoints.Count;
            }
            else
            {
                requiredSegments++;
            }
        }


        // =========================================================
        // LEASH: SHIP -> P
        // Tylko gdy P rzeczywiœcie reprezentuje progress/leash.
        // =========================================================

        bool firstIsAttackMove =
            commands.Count > 0 &&
            commands[0].Type ==
                ShipUnit.ShipCommandType.AttackMove;

        bool firstIsGuard =
            commands.Count > 0 &&
            commands[0].Type ==
                ShipUnit.ShipCommandType.Guard;

        bool showLeash =
            (
                firstIsAttackMove &&
                (
                    ship.CurrentState ==
                        ShipUnit.ShipState.Attacking ||
                    ship.AttackMoveReturningToProgress.Value
                )
            )
            ||
            (
                firstIsGuard &&
                guardPoints.Count > 1 &&
                (
                    ship.CurrentState ==
                        ShipUnit.ShipState.Attacking ||
                    ship.AttackMoveReturningToProgress.Value
                )
            );

        if (showLeash)
            requiredSegments++;


        EnsureSegmentCount(
            requiredSegments);


        Vector3 shipPosition =
            ship.transform.position;

        shipPosition.y =
            pathHeight;

        Vector3 startPosition =
            shipPosition;

        int segmentIndex = 0;


        // =========================================================
        // SHIP -> PROGRESS POINT
        // =========================================================

        if (showLeash)
        {
            Vector3 progressPoint =
                ship.AttackMoveProgressAnchor.Value;

            progressPoint.y =
                pathHeight;

            DrawSegment(
                segmentIndex++,
                shipPosition,
                progressPoint,
                commands[0].Type);

            startPosition =
                progressPoint;
        }


        // =========================================================
        // NORMALNE KOMENDY
        // =========================================================

        for (int i = 0; i < commands.Count; i++)
        {
            ShipUnit.VisualShipCommand command =
                commands[i];


            // =====================================================
            // GUARD
            // Jedna komenda -> wiele punktów.
            // =====================================================

            if (command.Type ==
                    ShipUnit.ShipCommandType.Guard &&
                guardPoints.Count > 0)
            {

                bool multiPointGuard =
                guardPoints.Count > 1;

                bool guardAlreadyStarted =
                    multiPointGuard &&
                    ship.CurrentState !=
                        ShipUnit.ShipState.Passive;


                for (int p = 0;
                     p < guardPoints.Count;
                     p++)
                {
                    Vector3 endPosition =
                        guardPoints[p];

                    endPosition.y =
                        pathHeight;

                    if (guardAlreadyStarted &&
                        p == 0)
                    {
                        startPosition =
                            endPosition;

                        continue;
                    }

                    DrawSegment(
                        segmentIndex++,
                        startPosition,
                        endPosition,
                        ShipUnit.ShipCommandType.Guard);

                    startPosition =
                        endPosition;
                }

                continue;
            }


            // =====================================================
            // MOVE / ATTACK / ATTACK MOVE / ...
            // =====================================================

            Vector3 normalEnd =
                command.GetCurrentPosition();

            normalEnd.y =
                pathHeight;

            DrawSegment(
                segmentIndex++,
                startPosition,
                normalEnd,
                command.Type);

            startPosition =
                normalEnd;
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

    private void RefreshAttackMoveProgressMarker()
    {
        if (attackMoveProgressMarker == null ||
            ship == null)
        {
            return;
        }

        IReadOnlyList<ShipUnit.VisualShipCommand> commands =
            ship.VisualCommands;

        if (commands.Count == 0)
        {
            attackMoveProgressMarker.gameObject.SetActive(false);
            return;
        }

        ShipUnit.ShipCommandType type =
            commands[0].Type;

        bool attackMove =
            type ==
            ShipUnit.ShipCommandType.AttackMove;

        bool guardPatrol =
            type ==
                ShipUnit.ShipCommandType.Guard &&
            ship.VisualGuardPoints.Count > 1;

        bool show =
            attackMove ||
            guardPatrol;

        if (!show)
        {
            attackMoveProgressMarker.gameObject.SetActive(false);
            return;
        }

        Vector3 position =
            ship.AttackMoveProgressAnchor.Value;

        position.y =
            pathHeight;

        attackMoveProgressMarker.position =
            position;

        attackMoveProgressMarker.gameObject.SetActive(true);
    }

    private void DrawSegment(
    int segmentIndex,
    Vector3 start,
    Vector3 end,
    ShipUnit.ShipCommandType type)
    {
        LineRenderer line =
            segments[segmentIndex];

        line.gameObject.SetActive(true);

        line.positionCount = 2;

        line.SetPosition(
            0,
            start);

        line.SetPosition(
            1,
            end);

        Color color =
            ShipCommandVisualSettings.Instance != null
                ? ShipCommandVisualSettings.Instance.GetColor(
                    type)
                : Color.white;

        line.startColor =
            color;

        line.endColor =
            color;
    }
}