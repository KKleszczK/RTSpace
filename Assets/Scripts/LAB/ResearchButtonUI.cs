using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResearchButtonUI :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField]
    private Image iconImage;

    [SerializeField]
    private Button button;

    private ResearchDefinition definition;
    private LabPanelUI labPanel;

    private ActionTooltipUI actionTooltip;
    private RectTransform rectTransform;

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

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        actionTooltip =
            FindFirstObjectByType<ActionTooltipUI>(
                FindObjectsInactive.Include);
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


        // =========================================================
        // DESCRIPTION
        // =========================================================

        labPanel.ShowDescription(
            definition);


        // =========================================================
        // COST TOOLTIP
        // =========================================================

        if (actionTooltip != null)
        {
            actionTooltip.Show(
                definition.baseMetalCost,
                definition.baseEnergyCost,
                definition.baseResearchTime,
                rectTransform);
        }
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

    public void OnPointerExit(
    PointerEventData eventData)
    {
        if (actionTooltip != null)
        {
            actionTooltip.Hide();
        }
    }
}