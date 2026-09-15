using TMPro;
using UnityEngine;

public class ActionTooltipUI : MonoBehaviour
{
    [Header("Values")]
    [SerializeField]
    private TMP_Text metalText;

    [SerializeField]
    private TMP_Text energyText;

    [SerializeField]
    private TMP_Text timeText;


    [Header("Position")]
    [SerializeField]
    private Vector2 offset =
        new Vector2(0f, 80f);


    private RectTransform rectTransform;
    private Canvas canvas;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvas =
            GetComponentInParent<Canvas>();

        Hide();
    }


    // =========================================================
    // SHOW
    // =========================================================

    public void Show(
        int metal,
        int energy,
        float time,
        RectTransform sourceButton)
    {
        if (sourceButton == null)
            return;


        // =====================================================
        // DATA
        // =====================================================

        if (metalText != null)
        {
            metalText.text =
                metal.ToString();
        }

        if (energyText != null)
        {
            energyText.text =
                energy.ToString();
        }

        if (timeText != null)
        {
            timeText.text =
                $"{time:0.#} s";
        }


        // =====================================================
        // SHOW
        // =====================================================

        gameObject.SetActive(true);


        // =====================================================
        // POSITION
        // =====================================================

        SetPosition(
            sourceButton);
    }


    // =========================================================
    // POSITION
    // =========================================================

    private void SetPosition(
    RectTransform sourceButton)
    {
        if (rectTransform == null ||
            canvas == null ||
            sourceButton == null)
        {
            return;
        }

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;


        // =========================================================
        // GÓRNY ŒRODEK PRZYCISKU W WORLD SPACE
        // =========================================================

        Vector3 buttonTopCenter =
            sourceButton.TransformPoint(
                new Vector3(
                    sourceButton.rect.center.x,
                    sourceButton.rect.yMax,
                    0f));


        // =========================================================
        // WORLD -> SCREEN
        // =========================================================

        Camera uiCamera =
            canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                buttonTopCenter);


        // =========================================================
        // SCREEN -> LOCAL POSITION W CANVAS
        // =========================================================

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 canvasPosition))
        {
            return;
        }


        // =========================================================
        // TOOLTIP NAD PRZYCISKIEM
        // =========================================================

        rectTransform.anchoredPosition =
            canvasPosition + offset;
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}