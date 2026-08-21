using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResearchButtonUI :
    MonoBehaviour,
    IPointerEnterHandler
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Button button;

    private ResearchDefinition definition;
    private LabPanelUI labPanel;

    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        ResearchDefinition newDefinition,
        LabPanelUI newLabPanel)
    {
        definition =
            newDefinition;

        labPanel =
            newLabPanel;

        Refresh();

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(() =>
        {
            labPanel.SelectResearch(
                definition);
        });
    }

    // =========================================================
    // HOVER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (definition == null ||
            labPanel == null)
        {
            return;
        }

        labPanel.ShowDescription(
            definition);
    }

    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (definition == null ||
            labPanel == null)
        {
            return;
        }

        bool completed =
            labPanel.IsResearchCompleted(
                definition.researchId);

        // -----------------------------------------
        // SPRITE
        // -----------------------------------------

        iconImage.sprite =
            completed &&
            definition.researchedIcon != null
                ? definition.researchedIcon
                : definition.icon;

        // -----------------------------------------
        // TIER COLOR
        // -----------------------------------------

        iconImage.color =
            ResearchTierColorHelper.GetColor(
            definition.tier);

        // -----------------------------------------
        // BUTTON
        // -----------------------------------------

        button.interactable =
            !completed;
    }
}