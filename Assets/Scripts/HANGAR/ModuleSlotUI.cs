using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModuleSlotUI :
    MonoBehaviour,
    IDropHandler,
    IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Sprite emptySprite;

    [SerializeField, Range(0, 3)]
    private int slotIndex;

    private HangarPanelUI hangarPanel;
    private ModuleDefinition currentModule;
    private bool isLocked;

    public int SlotIndex => slotIndex;
    public bool IsEmpty => currentModule == null;
    public bool IsLocked => isLocked;

    public void Setup(
        HangarPanelUI panel,
        int index)
    {
        hangarPanel = panel;
        slotIndex = index;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);
    }

    public void SetModule(
        ModuleDefinition module)
    {
        currentModule = module;

        if (iconImage != null)
        {
            iconImage.sprite =
                module != null
                    ? module.icon
                    : emptySprite;
        }
    }

    public ModuleDefinition GetModule()
    {
        return currentModule;
    }

    public void Clear()
    {
        SetModule(null);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (isLocked || hangarPanel == null)
            return;

        if (eventData.pointerDrag == null)
            return;

        DraggableModuleUI dragged =
            eventData.pointerDrag.GetComponent<DraggableModuleUI>();

        if (dragged == null)
            return;

        ModuleDefinition module = dragged.GetModule();

        if (module == null)
            return;

        Debug.Log(
            $"[MODULE DROP] module={module.moduleId}, slot={slotIndex}");

        hangarPanel.RequestInstallModule(
            slotIndex,
            module);
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (isLocked ||
            currentModule == null ||
            hangarPanel == null)
        {
            return;
        }

        // Prawy przycisk usuwa modu³.
        if (eventData.button ==
            PointerEventData.InputButton.Right)
        {
            hangarPanel.RequestRemoveModule(
                slotIndex);
        }
    }
}