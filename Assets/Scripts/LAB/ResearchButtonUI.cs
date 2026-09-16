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

    [SerializeField]
    private Image researchedOverlay;

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
        // SMART TOOLTIP
        // =========================================================

        if (actionTooltip != null)
        {
            actionTooltip.ShowResearch(
                definition,
                labPanel.GetCurrentCoreTier(),
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


        // =========================================================
        // STATE
        // =========================================================

        bool completed =
            labPanel.IsResearchCompleted(
                definition.researchId);

        bool available =
            labPanel.IsResearchAvailable(
                definition);

        bool locked =
            !completed &&
            !available;


        // =========================================================
        // SPRITE
        // =========================================================

        iconImage.sprite = definition.icon;

        // =========================================================
        // COMPLETED OVERLAY
        // =========================================================

        if (researchedOverlay != null)
        {
            researchedOverlay.gameObject.SetActive(
                completed);
        }



        // =========================================================
        // COLOR
        // =========================================================

        iconImage.color =
            ResearchTierColorHelper.GetColor(
                definition.tier,
                locked);


        // =========================================================
        // BUTTON
        // =========================================================

        button.interactable =
            true;
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