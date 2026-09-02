using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MinimapUI :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Map")]
    [SerializeField] private float mapSize = 100f;

    [Header("UI")]
    [SerializeField] private RectTransform markersContainer;

    [Header("Marker Prefabs")]
    [SerializeField] private RectTransform shipMarkerPrefab;
    [SerializeField] private RectTransform baseMarkerPrefab;

    [Header("Update")]
    [SerializeField] private float updateInterval = 0.1f;

    [Header("Camera Control")]
    [SerializeField]
    private RtsCameraController cameraController;


    private float nextUpdateTime;

    private readonly Dictionary<ShipUnit, RectTransform>
        shipMarkers = new();

    private readonly Dictionary<BaseUnit, RectTransform>
        baseMarkers = new();

    private void Update()
    {
        if (Time.time < nextUpdateTime)
            return;

        nextUpdateTime =
            Time.time + updateInterval;

        RefreshShips();
        RefreshBases();
    }

    // =========================================================
    // SHIPS
    // =========================================================

    private void RefreshShips()
    {
        ShipUnit[] ships =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        HashSet<ShipUnit> currentShips =
            new HashSet<ShipUnit>();

        foreach (ShipUnit ship in ships)
        {
            if (ship == null)
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            currentShips.Add(ship);

            if (!shipMarkers.TryGetValue(
                    ship,
                    out RectTransform marker))
            {
                marker =
                    CreateShipMarker(ship);

                if (marker == null)
                    continue;

                shipMarkers.Add(
                    ship,
                    marker);
            }

            UpdateMarkerPosition(
                marker,
                ship.transform.position);

            UpdateMarkerColor(
                marker,
                ship.ownerId.Value);
        }

        RemoveMissingShipMarkers(
            currentShips);
    }

    private RectTransform CreateShipMarker(
        ShipUnit ship)
    {
        if (shipMarkerPrefab == null ||
            markersContainer == null)
        {
            return null;
        }

        RectTransform marker =
            Instantiate(
                shipMarkerPrefab,
                markersContainer);

        marker.localScale =
            Vector3.one;

        marker.localRotation =
            Quaternion.identity;

        return marker;
    }

    private void RemoveMissingShipMarkers(
        HashSet<ShipUnit> currentShips)
    {
        List<ShipUnit> toRemove =
            new List<ShipUnit>();

        foreach (
            KeyValuePair<ShipUnit, RectTransform> pair
            in shipMarkers)
        {
            ShipUnit ship =
                pair.Key;

            if (ship == null ||
                !currentShips.Contains(ship))
            {
                if (pair.Value != null)
                {
                    Destroy(
                        pair.Value.gameObject);
                }

                toRemove.Add(ship);
            }
        }

        foreach (ShipUnit ship in toRemove)
        {
            shipMarkers.Remove(ship);
        }
    }

    // =========================================================
    // BASES
    // =========================================================

    private void RefreshBases()
    {
        BaseUnit[] bases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        HashSet<BaseUnit> currentBases =
            new HashSet<BaseUnit>();

        foreach (BaseUnit baseUnit in bases)
        {
            if (baseUnit == null)
                continue;

            if (!baseUnit.IsSpawned)
                continue;

            if (baseUnit.IsDead)
                continue;

            currentBases.Add(
                baseUnit);

            if (!baseMarkers.TryGetValue(
                    baseUnit,
                    out RectTransform marker))
            {
                marker =
                    CreateBaseMarker(
                        baseUnit);

                if (marker == null)
                    continue;

                baseMarkers.Add(
                    baseUnit,
                    marker);
            }

            UpdateMarkerPosition(
                marker,
                baseUnit.transform.position);

            UpdateMarkerColor(
                marker,
                baseUnit.OwnerId);
        }

        RemoveMissingBaseMarkers(
            currentBases);
    }

    private RectTransform CreateBaseMarker(
        BaseUnit baseUnit)
    {
        if (baseMarkerPrefab == null ||
            markersContainer == null)
        {
            return null;
        }

        RectTransform marker =
            Instantiate(
                baseMarkerPrefab,
                markersContainer);

        marker.localScale =
            Vector3.one;

        marker.localRotation =
            Quaternion.identity;

        return marker;
    }

    private void RemoveMissingBaseMarkers(
        HashSet<BaseUnit> currentBases)
    {
        List<BaseUnit> toRemove =
            new List<BaseUnit>();

        foreach (
            KeyValuePair<BaseUnit, RectTransform> pair
            in baseMarkers)
        {
            BaseUnit baseUnit =
                pair.Key;

            if (baseUnit == null ||
                !currentBases.Contains(baseUnit))
            {
                if (pair.Value != null)
                {
                    Destroy(
                        pair.Value.gameObject);
                }

                toRemove.Add(
                    baseUnit);
            }
        }

        foreach (BaseUnit baseUnit in toRemove)
        {
            baseMarkers.Remove(
                baseUnit);
        }
    }

    // =========================================================
    // POSITION
    // =========================================================

    private void UpdateMarkerPosition(
    RectTransform marker,
    Vector3 worldPosition)
    {
        if (marker == null ||
            markersContainer == null)
        {
            return;
        }

        marker.anchoredPosition =
            WorldToMinimapLocal(
                worldPosition);
    }


    private void UpdateMarkerColor(
    RectTransform marker,
    ulong ownerId)
    {
        if (marker == null)
            return;

        Image image =
            marker.GetComponent<Image>();

        if (image == null)
        {
            image =
                marker.GetComponentInChildren<Image>();
        }

        if (image == null)
            return;

        image.color =
            PlayerColorHelper.GetColor(
                ownerId);
    }


    public void OnPointerClick(
    PointerEventData eventData)
    {
        if (markersContainer == null ||
            cameraController == null)
        {
            return;
        }

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        Vector2 localPoint;

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    markersContainer,
                    eventData.position,
                    eventData.pressEventCamera,
                    out localPoint))
        {
            return;
        }

        Rect rect =
            markersContainer.rect;

        float normalizedX =
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPoint.x);

        float normalizedY =
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPoint.y);

        float halfMap =
            mapSize * 0.5f;

        float worldX =
            Mathf.Lerp(
                -halfMap,
                halfMap,
                normalizedX);

        float worldZ =
            Mathf.Lerp(
                -halfMap,
                halfMap,
                normalizedY);

        Vector3 worldPosition =
            new Vector3(
                worldX,
                0f,
                worldZ);

        cameraController
            .MoveViewToWorldPosition(
                worldPosition);
    }

    public Vector2 WorldToMinimapLocal(
    Vector3 worldPosition)
    {
        float halfMap =
            mapSize * 0.5f;

        float normalizedX =
            (worldPosition.x + halfMap) /
            mapSize;

        float normalizedY =
            (worldPosition.z + halfMap) /
            mapSize;

        normalizedX =
            Mathf.Clamp01(
                normalizedX);

        normalizedY =
            Mathf.Clamp01(
                normalizedY);

        float width =
            markersContainer.rect.width;

        float height =
            markersContainer.rect.height;

        float x =
            (
                normalizedX -
                markersContainer.pivot.x
            ) *
            width;

        float y =
            (
                normalizedY -
                markersContainer.pivot.y
            ) *
            height;

        return new Vector2(
            x,
            y);
    }
}