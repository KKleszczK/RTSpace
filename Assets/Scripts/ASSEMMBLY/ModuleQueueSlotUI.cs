using UnityEngine;
using UnityEngine.UI;

public class ModuleQueueSlotUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite emptySprite;

    private int index;
    private AssemblyPanelUI panel;

    public void Setup(int slotIndex, AssemblyPanelUI assemblyPanel)
    {
        index = slotIndex;
        panel = assemblyPanel;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => panel.RemoveQueueItem(index));
    }

    public void SetEmpty()
    {
        icon.sprite = emptySprite;
        button.interactable = false;
    }

    public void SetModule(ModuleDefinition module)
    {
        icon.sprite = module.icon;
        button.interactable = true;
    }
}