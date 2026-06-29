using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResearchButtonUI : MonoBehaviour
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
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        labPanel.SelectResearch(definition);
    }
}