using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModuleSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Sprite emptySprite;

    private ModuleDefinition currentModule;
    private bool isLocked;

    public bool IsEmpty => currentModule == null;
    public bool IsLocked => isLocked;

    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockOverlay != null)
            lockOverlay.SetActive(locked);
    }

    public void SetModule(ModuleDefinition module)
    {
        currentModule = module;

        if (iconImage != null)
            iconImage.sprite = module != null ? module.icon : emptySprite;
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
        if (isLocked)
            return;

        DraggableModuleUI dragged =
            eventData.pointerDrag.GetComponent<DraggableModuleUI>();

        if (dragged == null)
            return;

        ModuleDefinition module = dragged.GetModule();

        if (module == null)
            return;

        SetModule(module);

        dragged.OnDroppedIntoSlot(this);
    }
}