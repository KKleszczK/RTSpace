using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverBackgroundColor :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Target")]
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.gray;

    [Header("Return")]
    [SerializeField] private float returnTime = 0.25f;

    private bool isHovered;
    private float returnProgress;
    private Color returnStartColor;

    private void Awake()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color =
                normalColor;
        }
    }

    private void Update()
    {
        if (backgroundImage == null)
            return;

        if (isHovered)
            return;

        if (backgroundImage.color == normalColor)
            return;

        if (returnTime <= 0f)
        {
            backgroundImage.color =
                normalColor;

            return;
        }

        returnProgress +=
            Time.deltaTime / returnTime;

        backgroundImage.color =
            Color.Lerp(
                returnStartColor,
                normalColor,
                Mathf.Clamp01(returnProgress));
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (backgroundImage == null)
            return;

        isHovered = true;

        // Natychmiast kolor B.
        backgroundImage.color =
            hoverColor;
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
        if (backgroundImage == null)
            return;

        isHovered = false;

        returnProgress = 0f;

        returnStartColor =
            backgroundImage.color;
    }
}