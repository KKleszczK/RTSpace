using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableModuleUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [SerializeField] private Image iconImage;

    [Header("Drag appearance")]
    [SerializeField, Range(0f, 1f)]
    private float originalAlphaWhileDragging = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float ghostAlpha = 0.75f;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private ModuleDefinition module;

    private RectTransform dragGhost;
    private Image dragGhostImage;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();

        if (canvasGroup == null)
        {
            Debug.LogError(
                "DraggableModuleUI wymaga komponentu CanvasGroup.",
                gameObject);
        }

        if (canvas == null)
        {
            Debug.LogError(
                "DraggableModuleUI nie znajduje nadrzêdnego Canvas.",
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (module == null ||
            canvas == null ||
            canvasGroup == null ||
            iconImage == null)
        {
            return;
        }

        // Oryginalny element zostaje w Content.
        canvasGroup.alpha = originalAlphaWhileDragging;
        canvasGroup.blocksRaycasts = false;

        CreateDragGhost();

        if (dragGhost != null)
            dragGhost.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost == null)
            return;

        dragGhost.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        DestroyDragGhost();
    }

    private void CreateDragGhost()
    {
        GameObject ghostObject =
            new GameObject(
                "ModuleDragGhost",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));

        ghostObject.transform.SetParent(
            canvas.transform,
            false);

        ghostObject.transform.SetAsLastSibling();

        dragGhost =
            ghostObject.GetComponent<RectTransform>();

        dragGhostImage =
            ghostObject.GetComponent<Image>();

        CanvasGroup ghostCanvasGroup =
            ghostObject.GetComponent<CanvasGroup>();

        RectTransform sourceRect =
            iconImage.rectTransform;

        dragGhost.sizeDelta =
            sourceRect.rect.size;

        dragGhostImage.sprite =
            iconImage.sprite;

        dragGhostImage.preserveAspect = true;
        dragGhostImage.raycastTarget = false;

        ghostCanvasGroup.alpha = ghostAlpha;
        ghostCanvasGroup.blocksRaycasts = false;
        ghostCanvasGroup.interactable = false;
    }

    private void DestroyDragGhost()
    {
        if (dragGhost != null)
            Destroy(dragGhost.gameObject);

        dragGhost = null;
        dragGhostImage = null;
    }

    private void OnDisable()
    {
        DestroyDragGhost();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }
}