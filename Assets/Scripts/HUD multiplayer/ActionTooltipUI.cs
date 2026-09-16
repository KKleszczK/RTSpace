using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionTooltipUI : MonoBehaviour
{
    // =========================================================
    // VARIANT 1
    // Name + Costs + Requirements
    // =========================================================

    [Header("Variant 1 - Requirements")]

    [SerializeField]
    private GameObject variant1Panel;

    [SerializeField]
    private Image variant1TypeIcon;

    [SerializeField]
    private TMP_Text variant1NameText;

    [SerializeField]
    private TMP_Text variant1MetalText;

    [SerializeField]
    private TMP_Text variant1EnergyText;

    [SerializeField]
    private TMP_Text variant1TimeText;

    [SerializeField]
    private TMP_Text variant1RequirementsText;


    // =========================================================
    // VARIANT 2
    // Name + Costs + Description
    // =========================================================

    [Header("Variant 2 - Details")]

    [SerializeField]
    private GameObject variant2Panel;

    [SerializeField]
    private Image variant2TypeIcon;

    [SerializeField]
    private TMP_Text variant2NameText;

    [SerializeField]
    private TMP_Text variant2MetalText;

    [SerializeField]
    private TMP_Text variant2EnergyText;

    [SerializeField]
    private TMP_Text variant2TimeText;

    [SerializeField]
    private TMP_Text variant2DescriptionText;


    // =========================================================
    // VARIANT 3
    // Name + Costs + Requirements + Description
    // =========================================================

    [Header("Variant 3 - Requirements + Details")]

    [SerializeField]
    private GameObject variant3Panel;

    [SerializeField]
    private Image variant3TypeIcon;

    [SerializeField]
    private TMP_Text variant3NameText;

    [SerializeField]
    private TMP_Text variant3MetalText;

    [SerializeField]
    private TMP_Text variant3EnergyText;

    [SerializeField]
    private TMP_Text variant3TimeText;

    [SerializeField]
    private TMP_Text variant3RequirementsText;

    [SerializeField]
    private TMP_Text variant3DescriptionText;



    public enum TooltipVariant
    {
        Requirements,
        Details,
        RequirementsAndDetails
    }

    // =========================================================
    // COLORS
    // =========================================================

    [Header("Requirement / Unlock Colors")]

    [SerializeField]
    private Color requirementMetColor =
        new Color(
            0.25f,
            0.65f,
            1f,
            1f);

    [SerializeField]
    private Color requirementNotMetColor =
        new Color(
            1f,
            0.25f,
            0.25f,
            1f);

    [SerializeField]
    private Color unlockColor =
        new Color(
            0.25f,
            0.65f,
            1f,
            1f);


    [Header("Position")]
    [SerializeField]
    private Vector2 offset =
        new Vector2(0f, 80f);


    private RectTransform rectTransform;
    private Canvas canvas;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvas =
            GetComponentInParent<Canvas>();

        Hide();
    }

    private void ShowVariant(
    TooltipVariant variant)
    {
        if (variant1Panel != null)
        {
            variant1Panel.SetActive(
                variant ==
                TooltipVariant.Requirements);
        }

        if (variant2Panel != null)
        {
            variant2Panel.SetActive(
                variant ==
                TooltipVariant.Details);
        }

        if (variant3Panel != null)
        {
            variant3Panel.SetActive(
                variant ==
                TooltipVariant.RequirementsAndDetails);
        }
    }

    private TooltipVariant GetVariant(
    bool hasRequirements,
    bool hasUnlocks,
    bool hasDescription)
    {
        bool hasRequirementsContent =
            hasRequirements ||
            hasUnlocks;

        if (hasRequirementsContent &&
            hasDescription)
        {
            return TooltipVariant.RequirementsAndDetails;
        }

        if (hasRequirementsContent)
        {
            return TooltipVariant.Requirements;
        }

        return TooltipVariant.Details;
    }


    // =========================================================
    // SHOW
    // =========================================================

    public void Show(
        int metal,
        int energy,
        float time,
        RectTransform sourceButton)
    {
        
    }


    // =========================================================
    // POSITION
    // =========================================================

    private void SetPosition(
    RectTransform sourceButton)
    {
        if (rectTransform == null ||
            canvas == null ||
            sourceButton == null)
        {
            return;
        }

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;


        // =========================================================
        // GÓRNY ŒRODEK PRZYCISKU W WORLD SPACE
        // =========================================================

        Vector3 buttonTopCenter =
            sourceButton.TransformPoint(
                new Vector3(
                    sourceButton.rect.center.x,
                    sourceButton.rect.yMax,
                    0f));


        // =========================================================
        // WORLD -> SCREEN
        // =========================================================

        Camera uiCamera =
            canvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                buttonTopCenter);


        // =========================================================
        // SCREEN -> LOCAL POSITION W CANVAS
        // =========================================================

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                uiCamera,
                out Vector2 canvasPosition))
        {
            return;
        }


        // =========================================================
        // TOOLTIP NAD PRZYCISKIEM
        // =========================================================

        rectTransform.anchoredPosition =
            canvasPosition + offset;
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowResearch(
    ResearchDefinition definition,
    int currentCoreTier,
    RectTransform sourceButton)
    {
        if (definition == null ||
            sourceButton == null)
        {
            return;
        }


        // =========================================================
        // REQUIREMENTS
        // =========================================================

        bool hasRequirements =
            (int)definition.tier > 1;

        bool requirementMet =
            currentCoreTier >=
            (int)definition.tier;


        // =========================================================
        // UNLOCKS
        // =========================================================

        bool hasUnlocks =
            definition.unlockedModuleIds != null &&
            definition.unlockedModuleIds.Count > 0;


        // =========================================================
        // DESCRIPTION
        // =========================================================

        bool hasDescription =
            !string.IsNullOrWhiteSpace(
                definition.description);


        // =========================================================
        // VARIANT
        // =========================================================

        TooltipVariant variant =
            GetVariant(
                hasRequirements,
                hasUnlocks,
                hasDescription);

        ShowVariant(
            variant);


        // =========================================================
        // REQUIREMENTS + UNLOCKS TEXT
        // =========================================================

        string requirementsText =
            "";


        if (hasRequirements)
        {
            requirementsText +=
                GetRequirementText(
                    $"Requires: Base Core Tier {(int)definition.tier}",
                    requirementMet);
        }


        if (hasUnlocks)
        {
            foreach (string moduleId
                     in definition.unlockedModuleIds)
            {
                if (!string.IsNullOrEmpty(
                        requirementsText))
                {
                    requirementsText +=
                        "\n";
                }

                requirementsText +=
                    GetUnlockText(
                        $"Unlocks: {moduleId}");
            }
        }


        // =========================================================
        // FILL SELECTED VARIANT
        // =========================================================

        switch (variant)
        {
            case TooltipVariant.Requirements:

                FillVariant1(
                    definition,
                    requirementsText);

                break;


            case TooltipVariant.Details:

                FillVariant2(
                    definition);

                break;


            case TooltipVariant.RequirementsAndDetails:

                FillVariant3(
                    definition,
                    requirementsText);

                break;
        }


        // =========================================================
        // SHOW + POSITION
        // =========================================================

        gameObject.SetActive(true);

        SetPosition(
            sourceButton);
    }

    private void FillVariant1(
    ResearchDefinition definition,
    string requirements)
    {
        variant1TypeIcon.sprite =
            definition.icon;

        variant1NameText.text =
            definition.displayName;

        variant1MetalText.text =
            definition.baseMetalCost.ToString();

        variant1EnergyText.text =
            definition.baseEnergyCost.ToString();

        variant1TimeText.text =
            $"{definition.baseResearchTime:0.#} s";

        variant1RequirementsText.text =
            requirements;
    }

    private void FillVariant2(
    ResearchDefinition definition)
    {
        variant2TypeIcon.sprite =
            definition.icon;

        variant2NameText.text =
            definition.displayName;

        variant2MetalText.text =
            definition.baseMetalCost.ToString();

        variant2EnergyText.text =
            definition.baseEnergyCost.ToString();

        variant2TimeText.text =
            $"{definition.baseResearchTime:0.#} s";

        variant2DescriptionText.text =
            definition.description;
    }

    private void FillVariant3(
    ResearchDefinition definition,
    string requirements)
    {
        variant3TypeIcon.sprite =
            definition.icon;

        variant3NameText.text =
            definition.displayName;

        variant3MetalText.text =
            definition.baseMetalCost.ToString();

        variant3EnergyText.text =
            definition.baseEnergyCost.ToString();

        variant3TimeText.text =
            $"{definition.baseResearchTime:0.#} s";

        variant3RequirementsText.text =
            requirements;

        variant3DescriptionText.text =
            definition.description;
    }


    // =========================================================
    // REQUIREMENT / UNLOCK TEXT
    // =========================================================

    private string GetRequirementText(
        string text,
        bool fulfilled)
    {
        Color color =
            fulfilled
                ? requirementMetColor
                : requirementNotMetColor;

        return
            $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>" +
            $"{text}" +
            "</color>";
    }


    private string GetUnlockText(
        string text)
    {
        return
            $"<color=#{ColorUtility.ToHtmlStringRGBA(unlockColor)}>" +
            $"{text}" +
            "</color>";
    }
}