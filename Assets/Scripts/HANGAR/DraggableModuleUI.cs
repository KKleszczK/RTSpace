using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableModuleUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image iconImage;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;

    private Transform startParent;
    private Vector2 startPosition;

    private ModuleDefinition module;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Setup(ModuleDefinition newModule)
    {
        module = newModule;

        if (iconImage != null)
            iconImage.sprite = module.icon;
    }

    public ModuleDefinition GetModule()
    {
        return module;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startParent = transform.parent;
        startPosition = rectTransform.anchoredPosition;

        transform.SetParent(canvas.transform);

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        transform.SetParent(startParent);
        rectTransform.anchoredPosition = startPosition;
    }

    public void OnDroppedIntoSlot(ModuleSlotUI slot)
    {
        startParent = slot.transform;

        transform.SetParent(slot.transform);
        rectTransform.anchoredPosition = Vector2.zero;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }
}