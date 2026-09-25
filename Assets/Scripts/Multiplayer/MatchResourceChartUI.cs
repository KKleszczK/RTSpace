using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class MatchResourceChartUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler
{
    [SerializeField] private UILineRenderer localPlayerLine;
    [SerializeField] private UILineRenderer enemyPlayerLine;

    [Header("Y Axis")]
    [SerializeField] private TMP_Text value100Text;
    [SerializeField] private TMP_Text value75Text;
    [SerializeField] private TMP_Text value50Text;
    [SerializeField] private TMP_Text value25Text;
    [SerializeField] private TMP_Text value0Text;

    [Header("Hover")]
    [SerializeField] private RectTransform hoverLine;
    [SerializeField] private TMP_Text hoverTimeText;

    [Header("Settings")]
    [SerializeField] private float snapshotInterval = 30f;

    private RectTransform rectTransform;
    private int currentPointCount;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (hoverLine != null)
            hoverLine.gameObject.SetActive(false);
    }


    public void Draw(
        List<int> localValues,
        List<int> enemyValues)
    {
        if (localValues == null ||
            enemyValues == null)
            return;

        int pointCount =
            Mathf.Max(
                localValues.Count,
                enemyValues.Count);

        if (pointCount < 2)
            return;

        currentPointCount = pointCount;


        // Najwiêksza wartoœæ z obu graczy.
        int maxValue = 1;

        foreach (int value in localValues)
            maxValue = Mathf.Max(maxValue, value);

        foreach (int value in enemyValues)
            maxValue = Mathf.Max(maxValue, value);


        // Oœ Y.
        value100Text.text =
            maxValue.ToString();

        value75Text.text =
            Mathf.RoundToInt(maxValue * 0.75f).ToString();

        value50Text.text =
            Mathf.RoundToInt(maxValue * 0.50f).ToString();

        value25Text.text =
            Mathf.RoundToInt(maxValue * 0.25f).ToString();

        value0Text.text = "0";


        float width =
            rectTransform.rect.width;

        float height =
            rectTransform.rect.height;

        float stepX =
            width / (pointCount - 1);


        List<Vector2> localPoints =
            CreatePoints(
                localValues,
                maxValue,
                stepX,
                height);

        List<Vector2> enemyPoints =
            CreatePoints(
                enemyValues,
                maxValue,
                stepX,
                height);


        localPlayerLine.SetPoints(localPoints);
        enemyPlayerLine.SetPoints(enemyPoints);
    }


    private List<Vector2> CreatePoints(
        List<int> values,
        int maxValue,
        float stepX,
        float height)
    {
        List<Vector2> points = new();

        for (int i = 0; i < values.Count; i++)
        {
            float x =
                i * stepX;

            float normalized =
                values[i] / (float)maxValue;

            float y =
                normalized * height;

            points.Add(
                new Vector2(x, y));
        }

        return points;
    }


    // =========================================================
    // HOVER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (currentPointCount < 2)
            return;

        hoverLine.gameObject.SetActive(true);

        UpdateHover(eventData);
    }


    public void OnPointerMove(
        PointerEventData eventData)
    {
        if (currentPointCount < 2)
            return;

        UpdateHover(eventData);
    }


    public void OnPointerExit(
        PointerEventData eventData)
    {
        hoverLine.gameObject.SetActive(false);
    }


    private void UpdateHover(
        PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
            return;


        Rect rect =
            rectTransform.rect;

        float normalizedX =
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPoint.x);

        normalizedX =
            Mathf.Clamp01(normalizedX);


        // Pozycja pionowej linii.
        float x =
            Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                normalizedX);

        Vector2 position =
            hoverLine.anchoredPosition;

        position.x = x;

        hoverLine.anchoredPosition =
            position;


        // Czas odpowiadaj¹cy pozycji myszy.
        float totalSeconds =
            (currentPointCount - 1) *
            snapshotInterval;

        int seconds =
            Mathf.RoundToInt(
                normalizedX * totalSeconds);

        int minutes =
            seconds / 60;

        int remainingSeconds =
            seconds % 60;

        hoverTimeText.text =
            $"{minutes}:{remainingSeconds:00}";
    }
}