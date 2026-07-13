using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ModuleButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private ModuleDefinition module;
    private AssemblyPanelUI panel;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] ModuleButtonUI nie ma komponentu Button.",
                gameObject);
        }
    }

    public void Setup(
        ModuleDefinition newModule,
        AssemblyPanelUI newPanel)
    {
        module = newModule;
        panel = newPanel;

        if (module == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] ModuleButtonUI.Setup: module == null",
                gameObject);

            return;
        }

        if (panel == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] ModuleButtonUI.Setup: panel == null",
                gameObject);

            return;
        }

        if (iconImage == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] ModuleButtonUI: iconImage nie jest przypisany.",
                gameObject);
        }
        else
        {
            iconImage.sprite = module.icon;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (module == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] Klikniêto przycisk, ale module == null",
                gameObject);

            return;
        }

        if (panel == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] Klikniêto przycisk, ale panel == null",
                gameObject);

            return;
        }

        Debug.Log(
            "[CRAFT 01] Klikniêto modu³: " +
            module.moduleId +
            " | " +
            module.displayName);

        panel.SelectModule(module);
    }

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (module != null && panel != null)
            panel.ShowModuleInfo(module);
    }

    public void OnPointerExit(
        PointerEventData eventData)
    {
    }
}