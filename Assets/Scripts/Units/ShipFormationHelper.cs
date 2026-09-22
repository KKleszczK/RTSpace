using System.Collections.Generic;
using UnityEngine;

public static class ShipFormationHelper
{
    public static Dictionary<ShipUnit, Vector3> CreateFormation(
        List<ShipUnit> ships,
        Vector3 targetCenter,
        float spacing)
    {
        Dictionary<ShipUnit, Vector3> result =
            new Dictionary<ShipUnit, Vector3>();

        if (ships == null ||
            ships.Count == 0)
        {
            return result;
        }

        // =========================================================
        // VALID SHIPS
        // =========================================================

        List<ShipUnit> validShips =
            new List<ShipUnit>();

        foreach (ShipUnit ship in ships)
        {
            if (ship == null)
                continue;

            if (!ship.IsMine())
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            validShips.Add(ship);
        }

        if (validShips.Count == 0)
            return result;

        // =========================================================
        // GROUP CENTER
        // =========================================================

        Vector3 groupCenter =
            Vector3.zero;

        foreach (ShipUnit ship in validShips)
        {
            groupCenter +=
                ship.transform.position;
        }

        groupCenter /=
            validShips.Count;

        // =========================================================
        // FORMATION DIRECTION
        // =========================================================

        Vector3 forward =
            targetCenter - groupCenter;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 right =
            Vector3.Cross(
                Vector3.up,
                forward).normalized;

        // =========================================================
        // FORMATION SIZE
        // =========================================================

        int shipCount =
            validShips.Count;

        int columns =
            Mathf.CeilToInt(
                Mathf.Sqrt(shipCount));

        int rows =
            Mathf.CeilToInt(
                shipCount /
                (float)columns);

        // =========================================================
        // CREATE SLOTS
        // =========================================================

        List<Vector3> freeSlots =
            new List<Vector3>();

        for (int row = 0;
             row < rows;
             row++)
        {
            int remainingShips =
                shipCount -
                row * columns;

            int shipsInRow =
                Mathf.Min(
                    columns,
                    remainingShips);

            float rowWidth =
                (shipsInRow - 1) *
                spacing;

            for (int column = 0;
                 column < shipsInRow;
                 column++)
            {
                float horizontalOffset =
                    column * spacing -
                    rowWidth * 0.5f;

                float verticalOffset =
                    (
                        row -
                        (rows - 1) * 0.5f
                    ) * spacing;

                Vector3 slot =
                    targetCenter +
                    right * horizontalOffset +
                    forward * verticalOffset;

                freeSlots.Add(slot);
            }
        }

        // =========================================================
        // ASSIGN NEAREST FREE SLOT
        // =========================================================

        List<ShipUnit> remainingShipsList =
            new List<ShipUnit>(validShips);

        while (remainingShipsList.Count > 0 &&
               freeSlots.Count > 0)
        {
            float bestDistance =
                float.MaxValue;

            int bestShipIndex = -1;
            int bestSlotIndex = -1;

            for (int shipIndex = 0;
                 shipIndex < remainingShipsList.Count;
                 shipIndex++)
            {
                Vector3 shipPosition =
                    remainingShipsList[shipIndex]
                        .transform.position;

                for (int slotIndex = 0;
                     slotIndex < freeSlots.Count;
                     slotIndex++)
                {
                    float distance =
                        (
                            shipPosition -
                            freeSlots[slotIndex]
                        ).sqrMagnitude;

                    if (distance >= bestDistance)
                        continue;

                    bestDistance =
                        distance;

                    bestShipIndex =
                        shipIndex;

                    bestSlotIndex =
                        slotIndex;
                }
            }

            if (bestShipIndex < 0 ||
                bestSlotIndex < 0)
            {
                break;
            }

            ShipUnit selectedShip =
                remainingShipsList[
                    bestShipIndex];

            Vector3 selectedSlot =
                freeSlots[
                    bestSlotIndex];

            result.Add(
                selectedShip,
                selectedSlot);

            remainingShipsList.RemoveAt(
                bestShipIndex);

            freeSlots.RemoveAt(
                bestSlotIndex);
        }

        return result;
    }
}