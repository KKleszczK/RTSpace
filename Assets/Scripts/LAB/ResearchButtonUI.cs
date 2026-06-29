using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResearchButtonUI : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Button button;

    private ResearchDefinition definition;
    private LabPanelUI labPanel;

    public void Setup(ResearchDefinition newDefinition, LabPanelUI newLabPanel)
    {
        definition = newDefinition;
        labPanel = newLabPanel;

        iconImage.sprite = definition.icon;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            labPanel.SelectResearch(definition);
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        labPanel.ShowDescription(definition);
    }

    public void Refresh()
    {
        bool completed = labPanel.IsResearchCompleted(definition.researchId);

        iconImage.sprite = completed && definition.researchedIcon != null
            ? definition.researchedIcon
            : definition.icon;

        button.interactable = !completed;
    }
}