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
        rectTransform =
            GetComponent<RectTransform>();

        canvasGroup =
            GetComponent<CanvasGroup>();

        canvas =
            GetComponentInParent<Canvas>();

        if (canvasGroup == null)
        {
            Debug.LogError(
                "DraggableModuleUI wymaga CanvasGroup.",
                gameObject);
        }
    }

    public void Setup(ModuleDefinition newModule)
    {
        module = newModule;

        if (iconImage != null)
        {
            iconImage.sprite =
                module != null
                    ? module.icon
                    : null;
        }
    }

    public ModuleDefinition GetModule()
    {
        return module;
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (module == null ||
            canvas == null ||
            canvasGroup == null)
        {
            return;
        }

        startParent = transform.parent;
        startPosition =
            rectTransform.anchoredPosition;

        transform.SetParent(
            canvas.transform,
            true);

        transform.SetAsLastSibling();

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        if (canvas == null)
            return;

        rectTransform.position =
            eventData.position;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        if (startParent != null)
        {
            transform.SetParent(
                startParent,
                false);

            rectTransform.anchoredPosition =
                startPosition;
        }
    }
}