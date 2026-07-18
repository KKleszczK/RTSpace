using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModuleSlotUI :
    MonoBehaviour,
    IDropHandler,
    IPointerClickHandler,
    IInitializePotentialDragHandler,
    IPointerDownHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Slot")]
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Sprite emptySprite;

    [SerializeField, Range(0, 3)]
    private int slotIndex;

    [Header("Drag")]
    [SerializeField, Range(0f, 1f)]
    private float originalIconAlphaWhileDragging = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float ghostAlpha = 0.8f;

    private HangarPanelUI hangarPanel;
    private ModuleDefinition currentModule;

    private bool isLocked;
    private bool isDragging;

    private Canvas rootCanvas;
    private CanvasGroup slotCanvasGroup;

    private RectTransform dragGhost;
    private Image dragGhostImage;

    private Color originalIconColor = Color.white;

    public int SlotIndex => slotIndex;
    public bool IsEmpty => currentModule == null;
    public bool IsLocked => isLocked;
    public bool IsDragging => isDragging;

    private void Awake()
    {
        FindCanvas();
        FindCanvasGroup();

        if (iconImage != null)
            originalIconColor = iconImage.color;
    }

    private void FindCanvas()
    {
        Canvas foundCanvas = GetComponentInParent<Canvas>(true);

        if (foundCanvas != null)
            rootCanvas = foundCanvas.rootCanvas;
    }

    private void FindCanvasGroup()
    {
        slotCanvasGroup = GetComponent<CanvasGroup>();

        if (slotCanvasGroup == null)
            slotCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(
        HangarPanelUI panel,
        int index)
    {
        hangarPanel = panel;
        slotIndex = index;

        if (rootCanvas == null)
            FindCanvas();

        if (slotCanvasGroup == null)
            FindCanvasGroup();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);
    }

    public void SetModule(ModuleDefinition module)
    {
        currentModule = module;

        if (iconImage == null)
            return;

        iconImage.sprite =
            module != null
                ? module.icon
                : emptySprite;

        if (!isDragging)
            iconImage.color = originalIconColor;
    }

    public ModuleDefinition GetModule()
    {
        return currentModule;
    }

    public void Clear()
    {
        SetModule(null);
    }

    // =========================================================
    // WK£ADANIE MODU£U Z INVENTORY DO SLOTU
    // =========================================================

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log(
            $"[MODULE SLOT DROP] slot={slotIndex}, " +
            $"locked={isLocked}, " +
            $"pointerDrag={(eventData.pointerDrag != null ? eventData.pointerDrag.name : "NULL")}");

        if (isLocked || hangarPanel == null)
            return;

        if (eventData.pointerDrag == null)
            return;

        DraggableModuleUI draggedModule =
            eventData.pointerDrag.GetComponent<DraggableModuleUI>();

        if (draggedModule == null)
        {
            draggedModule =
                eventData.pointerDrag.GetComponentInParent<DraggableModuleUI>();
        }

        if (draggedModule == null)
        {
            Debug.LogWarning(
                "[MODULE SLOT DROP] Przeci¹gany obiekt nie posiada DraggableModuleUI.");

            return;
        }

        ModuleDefinition module =
            draggedModule.GetModule();

        if (module == null)
        {
            Debug.LogWarning(
                "[MODULE SLOT DROP] DraggableModuleUI nie posiada modu³u.");

            return;
        }

        Debug.Log(
            $"[MODULE INSTALL REQUEST] " +
            $"module={module.moduleId}, slot={slotIndex}");

        hangarPanel.RequestInstallModule(
            slotIndex,
            module);
    }

    // =========================================================
    // ZDEJMOWANIE MODU£U PRAWYM PRZYCISKIEM
    // =========================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (isLocked ||
            currentModule == null ||
            hangarPanel == null)
        {
            return;
        }

        if (eventData.button ==
            PointerEventData.InputButton.Right)
        {
            RequestRemoveFromShip();
        }
    }

    // =========================================================
    // DRAG MODU£U ZE STATKU
    // =========================================================

    public void OnInitializePotentialDrag(
        PointerEventData eventData)
    {
        eventData.useDragThreshold = false;

        Debug.Log(
            $"[SLOT DRAG INITIALIZE] slot={slotIndex}");
    }

    public void OnPointerDown(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[SLOT POINTER DOWN] " +
            $"slot={slotIndex}, " +
            $"button={eventData.button}, " +
            $"module={(currentModule != null ? currentModule.moduleId : "BRAK")}");
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[SLOT DRAG BEGIN] " +
            $"slot={slotIndex}, " +
            $"button={eventData.button}, " +
            $"locked={isLocked}, " +
            $"module={currentModule != null}, " +
            $"panel={hangarPanel != null}, " +
            $"canvas={rootCanvas != null}, " +
            $"icon={iconImage != null}");

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            Debug.LogWarning(
                "[SLOT DRAG BLOCKED] Drag mo¿liwy tylko lewym przyciskiem.");

            return;
        }

        if (isLocked ||
            currentModule == null ||
            hangarPanel == null ||
            iconImage == null)
        {
            Debug.LogWarning(
                "[SLOT DRAG BLOCKED] Brak wymaganych danych.");

            return;
        }

        if (rootCanvas == null)
            FindCanvas();

        if (rootCanvas == null)
        {
            Debug.LogError(
                "[SLOT DRAG BLOCKED] Nie znaleziono Canvas.");

            return;
        }

        if (slotCanvasGroup == null)
            FindCanvasGroup();

        isDragging = true;

        if (slotCanvasGroup != null)
            slotCanvasGroup.blocksRaycasts = false;

        Color dimmedColor = originalIconColor;
        dimmedColor.a = originalIconAlphaWhileDragging;

        iconImage.color = dimmedColor;

        CreateDragGhost();

        MoveDragGhost(eventData);

        Debug.Log(
            $"[SLOT DRAG STARTED] " +
            $"slot={slotIndex}, " +
            $"dragging={isDragging}, " +
            $"ghost={dragGhost != null}");
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[SLOT DRAG MOVE] " +
            $"slot={slotIndex}, " +
            $"dragging={isDragging}, " +
            $"dragGhost={dragGhost != null}");

        if (!isDragging)
            return;

        if (dragGhost == null)
        {
            Debug.LogWarning(
                "[SLOT DRAG MOVE] Brak ghosta. Tworzê ponownie.");

            CreateDragGhost();
        }

        MoveDragGhost(eventData);
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        Debug.Log(
            $"[SLOT DRAG END] " +
            $"slot={slotIndex}, " +
            $"dragging={isDragging}, " +
            $"pointerEnter=" +
            $"{(eventData.pointerEnter != null ? eventData.pointerEnter.name : "NULL")}");

        ResetDragState();
    }

    private void MoveDragGhost(
        PointerEventData eventData)
    {
        if (dragGhost == null ||
            rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect =
            rootCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            dragGhost.position = eventData.position;
            return;
        }

        Camera eventCamera = null;

        if (rootCanvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = rootCanvas.worldCamera;
        }

        bool converted =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventCamera,
                out Vector2 localPoint);

        if (converted)
            dragGhost.anchoredPosition = localPoint;
        else
            dragGhost.position = eventData.position;
    }

    public void RequestRemoveFromShip()
    {
        if (isLocked ||
            currentModule == null ||
            hangarPanel == null)
        {
            return;
        }

        Debug.Log(
            $"[MODULE REMOVE REQUEST] " +
            $"module={currentModule.moduleId}, " +
            $"slot={slotIndex}");

        hangarPanel.RequestRemoveModule(slotIndex);
    }

    // =========================================================
    // GHOST PRZECI¥GANEGO MODU£U
    // =========================================================

    private void CreateDragGhost()
    {
        DestroyDragGhost();

        if (rootCanvas == null ||
            iconImage == null ||
            iconImage.sprite == null)
        {
            Debug.LogWarning(
                "[SLOT GHOST] Nie mo¿na utworzyæ ghosta.");

            return;
        }

        GameObject ghostObject =
            new GameObject(
                "InstalledModuleDragGhost",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image));

        ghostObject.transform.SetParent(
            rootCanvas.transform,
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

        dragGhost.anchorMin =
            new Vector2(0.5f, 0.5f);

        dragGhost.anchorMax =
            new Vector2(0.5f, 0.5f);

        dragGhost.pivot =
            new Vector2(0.5f, 0.5f);

        Vector2 sourceSize =
            sourceRect.rect.size;

        if (sourceSize.x <= 0f ||
            sourceSize.y <= 0f)
        {
            sourceSize = sourceRect.sizeDelta;
        }

        if (sourceSize.x <= 0f ||
            sourceSize.y <= 0f)
        {
            sourceSize = new Vector2(64f, 64f);
        }

        dragGhost.sizeDelta = sourceSize;
        dragGhost.localScale = Vector3.one;
        dragGhost.localRotation = Quaternion.identity;

        dragGhostImage.sprite =
            iconImage.sprite;

        dragGhostImage.color = Color.white;
        dragGhostImage.preserveAspect = true;
        dragGhostImage.raycastTarget = false;

        ghostCanvasGroup.alpha = ghostAlpha;
        ghostCanvasGroup.blocksRaycasts = false;
        ghostCanvasGroup.interactable = false;

        Debug.Log(
            $"[SLOT GHOST CREATED] " +
            $"slot={slotIndex}, size={sourceSize}");
    }

    private void DestroyDragGhost()
    {
        if (dragGhost != null)
            Destroy(dragGhost.gameObject);

        dragGhost = null;
        dragGhostImage = null;
    }

    private void ResetDragState()
    {
        isDragging = false;

        DestroyDragGhost();

        if (slotCanvasGroup != null)
            slotCanvasGroup.blocksRaycasts = true;

        if (iconImage != null)
            iconImage.color = originalIconColor;
    }

    private void OnDisable()
    {
        Debug.LogWarning(
            $"[SLOT DISABLED] " +
            $"slot={slotIndex}, dragging={isDragging}");

        /*
         * Panel hangaru mo¿e chwilowo wy³¹czaæ slot podczas odœwie¿ania.
         * Nie resetujemy wtedy aktywnego draga, poniewa¿ powodowa³o to:
         *
         * dragging=False
         * dragGhost=False
         */
        if (!isDragging)
            ResetDragState();
    }

    private void OnDestroy()
    {
        DestroyDragGhost();
    }
}