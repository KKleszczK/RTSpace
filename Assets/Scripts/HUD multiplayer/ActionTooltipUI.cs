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

    [Header("Type Icons")]

    [SerializeField] private Sprite minerTypeIcon;
    [SerializeField] private Sprite fighterTypeIcon;
    [SerializeField] private Sprite utilityTypeIcon;

    [SerializeField] private Sprite researchTypeIcon;

    [SerializeField] private Sprite baseTier2TypeIcon;
    [SerializeField] private Sprite baseTier3TypeIcon;


    [Header("Screen Margin")]
    [SerializeField]
    private float screenMargin =
    10f;

    [Header("Cost Colors")]

    [SerializeField]
    private Color normalCostColor =
    Color.white;

    [SerializeField]
    private Color insufficientCostColor =
        Color.red;

    private PlayerResources currentPlayerResources;
    private ResearchDefinition currentResearch;
    private ShipDefinition currentShip;

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


        // =========================================================
        // KEEP TOOLTIP INSIDE CANVAS
        // =========================================================

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            rectTransform);

        Canvas.ForceUpdateCanvases();

        Vector3[] tooltipCorners =
            new Vector3[4];

        rectTransform.GetWorldCorners(
            tooltipCorners);

        Vector3[] canvasCorners =
            new Vector3[4];

        canvasRect.GetWorldCorners(
            canvasCorners);


        Vector3 correction =
            Vector3.zero;


        // LEFT
        if (tooltipCorners[0].x <
            canvasCorners[0].x + screenMargin)
        {
            correction.x +=
                canvasCorners[0].x +
                screenMargin -
                tooltipCorners[0].x;
        }


        // RIGHT
        if (tooltipCorners[2].x >
            canvasCorners[2].x - screenMargin)
        {
            correction.x -=
                tooltipCorners[2].x -
                (canvasCorners[2].x - screenMargin);
        }


        // BOTTOM
        if (tooltipCorners[0].y <
            canvasCorners[0].y + screenMargin)
        {
            correction.y +=
                canvasCorners[0].y +
                screenMargin -
                tooltipCorners[0].y;
        }


        // TOP
        if (tooltipCorners[2].y >
            canvasCorners[2].y - screenMargin)
        {
            correction.y -=
                tooltipCorners[2].y -
                (canvasCorners[2].y - screenMargin);
        }


        rectTransform.position +=
            correction;
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        UnsubscribeResources();

        currentPlayerResources = null;
        currentResearch = null;
        currentShip = null;

        gameObject.SetActive(false);
    }

    public void ShowResearch(
    ResearchDefinition definition,
    int currentCoreTier,
    PlayerResources playerResources,
    RectTransform sourceButton)
    {
        if (definition == null ||
            sourceButton == null)
        {
            return;
        }

        UnsubscribeResources();

        currentResearch =
            definition;

        currentPlayerResources =
            playerResources;

        SubscribeResources();




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
            definition.unlockedModuleIds.Exists(
                id => !string.IsNullOrWhiteSpace(id));




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
                    $"Base Core Tier {(int)definition.tier}",
                    requirementMet);
        }


        if (hasUnlocks)
        {
            foreach (string moduleId
                     in definition.unlockedModuleIds)
            {
                if (string.IsNullOrWhiteSpace(moduleId))
                    continue;

                ModuleDefinition module =
                    ModuleDatabase.Instance.GetModule(moduleId);

                if (module == null)
                    continue;

                if (!string.IsNullOrEmpty(
                        requirementsText))
                {
                    requirementsText += "\n";
                }

                requirementsText +=
                    GetUnlockText(module.displayName);
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

        RefreshCostColors();

        gameObject.SetActive(true);

        SetPosition(
            sourceButton);
    }

    private void FillVariant1(
    ResearchDefinition definition,
    string requirements)
    {
        variant1TypeIcon.sprite =
            researchTypeIcon;

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
            researchTypeIcon;

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
            researchTypeIcon;

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


    private void SubscribeResources()
    {
        if (currentPlayerResources == null)
            return;

        currentPlayerResources.metal.OnValueChanged +=
            OnResourcesChanged;

        currentPlayerResources.energy.OnValueChanged +=
            OnResourcesChanged;
    }


    private void UnsubscribeResources()
    {
        if (currentPlayerResources == null)
            return;

        currentPlayerResources.metal.OnValueChanged -=
            OnResourcesChanged;

        currentPlayerResources.energy.OnValueChanged -=
            OnResourcesChanged;
    }


    private void OnResourcesChanged(
        int previousValue,
        int newValue)
    {
        RefreshCostColors();
    }

    private void RefreshCostColors()
    {
        if (currentPlayerResources == null)
            return;

        int metalCost;
        int energyCost;

        if (currentResearch != null)
        {
            metalCost =
                currentResearch.baseMetalCost;

            energyCost =
                currentResearch.baseEnergyCost;
        }
        else if (currentShip != null)
        {
            metalCost =
                currentShip.metalCost;

            energyCost =
                currentShip.energyCost;
        }
        else
        {
            return;
        }

        bool enoughMetal =
            currentPlayerResources.metal.Value >=
            metalCost;

        bool enoughEnergy =
            currentPlayerResources.energy.Value >=
            energyCost;

        Color metalColor =
            enoughMetal
                ? normalCostColor
                : insufficientCostColor;

        Color energyColor =
            enoughEnergy
                ? normalCostColor
                : insufficientCostColor;

        if (variant1MetalText != null)
            variant1MetalText.color = metalColor;

        if (variant1EnergyText != null)
            variant1EnergyText.color = energyColor;

        if (variant2MetalText != null)
            variant2MetalText.color = metalColor;

        if (variant2EnergyText != null)
            variant2EnergyText.color = energyColor;

        if (variant3MetalText != null)
            variant3MetalText.color = metalColor;

        if (variant3EnergyText != null)
            variant3EnergyText.color = energyColor;
    }


    public void ShowShip(
    ShipDefinition definition,
    PlayerResources playerResources,
    RectTransform sourceButton)
    {
        if (definition == null ||
            sourceButton == null)
        {
            return;
        }

        UnsubscribeResources();

        currentResearch = null;
        currentShip = definition;
        currentPlayerResources = playerResources;
        SubscribeResources();
        RefreshCostColors();

        ShowVariant(
            TooltipVariant.Details);

        switch (definition.shipType)
        {
            case ShipType.Miner:
                variant2TypeIcon.sprite =
                    minerTypeIcon;
                break;

            case ShipType.Fighter:
                variant2TypeIcon.sprite =
                    fighterTypeIcon;
                break;

            case ShipType.Utility:
                variant2TypeIcon.sprite =
                    utilityTypeIcon;
                break;
        }

        variant2NameText.text =
            definition.displayName;

        variant2MetalText.text =
            definition.metalCost.ToString();

        variant2EnergyText.text =
            definition.energyCost.ToString();

        variant2TimeText.text =
            $"{definition.buildTime:0.#} s";

        variant2DescriptionText.text =
            definition.description;

        gameObject.SetActive(true);

        SetPosition(
            sourceButton);
    }


}