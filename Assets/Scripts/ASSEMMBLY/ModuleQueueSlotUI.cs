using UnityEngine;
using UnityEngine.UI;

public class ModuleQueueSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite emptySprite;

    private int index;
    private AssemblyPanelUI panel;

    public void Setup(
        int slotIndex,
        AssemblyPanelUI assemblyPanel)
    {
        index = slotIndex;
        panel = assemblyPanel;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => panel.RemoveQueueItem(index));
        }

        SetEmpty();
    }

    public void SetEmpty()
    {
        if (icon != null)
        {
            icon.sprite = emptySprite;
            icon.color = Color.white;
        }

        if (button != null)
            button.interactable = false;
    }

    public void SetModule(
        ModuleDefinition module)
    {
        if (module == null)
        {
            SetEmpty();
            return;
        }

        if (icon != null)
        {
            ModuleTierColorHelper.ApplyToImage(
                icon,
                module);
        }

        if (button != null)
            button.interactable = true;
    }
}