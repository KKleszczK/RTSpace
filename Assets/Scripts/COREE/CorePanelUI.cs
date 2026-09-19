using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CorePanelUI : MonoBehaviour
{
    [SerializeField] private Button constructButton;
    [SerializeField] private TMP_Text constructButtonText;
    [SerializeField] private TMP_Text constructButtonTextMetal;
    [SerializeField] private TMP_Text constructButtonTextEnergy;
    [SerializeField] private TMP_Text constructButtonTextTime;
    [SerializeField] private Image iconM;
    [SerializeField] private Image iconE;
    [SerializeField] private TMP_Text iconT;
    [SerializeField] private InteractiveProgressBarUI progressBar;

    [Header("Tooltip")]
    [SerializeField] private ActionTooltipUI actionTooltip;

    [Header("Tier 2 Tooltip")]
    [SerializeField] private string tier2Name = "Core Tier 2";

    [TextArea(3, 6)]
    [SerializeField] private string tier2Description;

    [SerializeField]
    private List<string> tier2Unlocks = new()
        {
            "Research Tier 2",
            "Modules Tier 2"
        };

    [Header("Tier 3 Tooltip")]
    [SerializeField] private string tier3Name = "Core Tier 3";

    [TextArea(3, 6)]
    [SerializeField] private string tier3Description;

    [SerializeField]
    private List<string> tier3Unlocks = new()
        {
            "Research Tier 3",
            "Modules Tier 3"
        };

    private PlayerResources localPlayerResources;

    private BaseCore selectedCore;

    public void SetCore(BaseCore core)
    {
        selectedCore = core;
    }

    private void Start()
    {
        constructButton.onClick.AddListener(() =>
        {
            if (selectedCore != null)
                selectedCore.RequestUpgrade();
        });

        TryFindLocalPlayerResources();

        EventTrigger trigger =
            constructButton.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                constructButton.gameObject
                    .AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers =
                new System.Collections.Generic.List<
                    EventTrigger.Entry>();
        }

        EventTrigger.Entry enter =
            new EventTrigger.Entry
            {
                eventID =
                    EventTriggerType.PointerEnter
            };

        enter.callback.AddListener(
            _ => ShowUpgradeTooltip());

        trigger.triggers.Add(enter);


        EventTrigger.Entry exit =
            new EventTrigger.Entry
            {
                eventID =
                    EventTriggerType.PointerExit
            };

        exit.callback.AddListener(
            _ => HideUpgradeTooltip());

        trigger.triggers.Add(exit);
    }

    private void Update()
    {

        if (localPlayerResources == null)
            TryFindLocalPlayerResources();


        if (selectedCore == null)
            return;

        int tier = selectedCore.tier.Value;

        if (tier < 3)
        {
            constructButtonText.text =
                $"CONSTRUCT T{tier + 1}";

            constructButtonTextMetal.text =
                selectedCore.GetNextUpgradeMetalCost().ToString();

            constructButtonTextEnergy.text =
                selectedCore.GetNextUpgradeEnergyCost().ToString();

            constructButtonTextTime.text =
                $"{selectedCore.GetNextUpgradeTime():0.#}";
        }
        else
        {
            constructButtonText.text = "CORE MAX";

            constructButtonTextMetal.gameObject.SetActive(false);
            constructButtonTextEnergy.gameObject.SetActive(false);
            constructButtonTextTime.gameObject.SetActive(false);

            iconM.gameObject.SetActive(false);
            iconE.gameObject.SetActive(false);
            iconT.gameObject.SetActive(false);
        }


        constructButton.interactable =
            tier < 3 && !selectedCore.isUpgrading.Value;

        if (progressBar != null)
        {
            progressBar.SetBonus(0f);

            progressBar.SetProgress(
                selectedCore.progress.Value);
        }
    }

    private void TryFindLocalPlayerResources()
    {
        if (localPlayerResources != null)
            return;

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return;

        PlayerResources[] all =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources resources in all)
        {
            if (resources == null)
                continue;

            if (!resources.IsSpawned)
                continue;

            if (resources.OwnerClientId !=
                Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                continue;
            }

            localPlayerResources = resources;
            return;
        }
    }

    private void ShowUpgradeTooltip()
    {
        if (actionTooltip == null ||
            selectedCore == null)
        {
            return;
        }

        int tier =
            selectedCore.tier.Value;

        // Tier 3 póŸniej bêdzie mia³ specjalne okno.
        if (tier >= 3)
            return;

        if (localPlayerResources == null)
            TryFindLocalPlayerResources();

        int targetTier =
            tier + 1;

        string displayName;
        string description;
        List<string> unlocks;

        if (targetTier == 2)
        {
            displayName = tier2Name;
            description = tier2Description;
            unlocks = tier2Unlocks;
        }
        else
        {
            displayName = tier3Name;
            description = tier3Description;
            unlocks = tier3Unlocks;
        }

        actionTooltip.ShowBaseUpgrade(
            targetTier,
            displayName,
            description,
            selectedCore.GetNextUpgradeMetalCost(),
            selectedCore.GetNextUpgradeEnergyCost(),
            selectedCore.GetNextUpgradeTime(),
            unlocks,
            localPlayerResources,
            constructButton.GetComponent<RectTransform>());
    }

    private void HideUpgradeTooltip()
    {
        if (actionTooltip != null)
            actionTooltip.Hide();
    }
}