using System.Collections.Generic;
using UnityEngine;

public class LabPanelUI : MonoBehaviour
{
    [Header("Research")]
    [SerializeField] private List<ResearchDefinition> researches = new();

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private ResearchButtonUI buttonPrefab;

    private ResearchDefinition selectedResearch;

    private void Start()
    {
        CreateButtons();
    }

    private void CreateButtons()
    {
        foreach (ResearchDefinition research in researches)
        {
            ResearchButtonUI button =
                Instantiate(buttonPrefab, content);

            button.Setup(research, this);
        }
    }

    public void SelectResearch(ResearchDefinition research)
    {
        selectedResearch = research;

        Debug.Log("Selected: " + research.displayName);
    }
}