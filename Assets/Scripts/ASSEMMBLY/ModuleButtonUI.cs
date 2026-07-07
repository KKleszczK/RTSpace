using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModuleButtonUI : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private ModuleDefinition module;
    private AssemblyPanelUI assemblyPanel;

    public void Setup(ModuleDefinition newModule, AssemblyPanelUI newPanel)
    {
        module = newModule;
        assemblyPanel = newPanel;

        iconImage.sprite = module.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (assemblyPanel != null)
            assemblyPanel.SelectModule(module);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (assemblyPanel != null)
            assemblyPanel.ShowModuleInfo(module);
    }
}