using UnityEngine;

public class NotificationManager : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField]
    private NotificationPopupUI popupPrefab;

    [SerializeField]
    private Transform popupContainer;

    [Header("Core Upgrade")]
    [SerializeField] private Sprite coreTier2Icon;
    [SerializeField] private Sprite coreTier3Icon;

    // =========================================================
    // MODULE
    // =========================================================

    public void ShowModuleCompleted(
        ModuleDefinition module)
    {
        if (module == null)
            return;

        Color tierColor =
            ModuleTierColorHelper.GetColor(
                module.tier);

        CreatePopup(
            module.displayName,
            GetModuleTierText(module.tier),
            module.icon,
            tierColor);
    }

    // =========================================================
    // RESEARCH
    // =========================================================

    public void ShowResearchCompleted(
        ResearchDefinition research)
    {
        if (research == null)
            return;

        Color tierColor =
            ResearchTierColorHelper.GetColor(
                research.tier);

        CreatePopup(
            research.displayName,
            GetResearchTierText(research.tier),
            research.icon,
            tierColor);
    }

    // =========================================================
    // CORE UPGRADE
    // =========================================================

    public void ShowCoreUpgrade(
        int tier)
    {
        Sprite icon;

        switch (tier)
        {
            case 2:
                icon = coreTier2Icon;
                break;

            case 3:
                icon = coreTier3Icon;
                break;

            default:
                Debug.LogWarning(
                    $"[NOTIFICATION] Nieobs³ugiwany Core Tier: {tier}");

                return;
        }

        CreatePopup(
            "CORE UPGRADE",
            tier.ToString(),
            icon,
            Color.white);
    }

    // =========================================================
    // CREATE
    // =========================================================

    private void CreatePopup(
        string itemName,
        string tierText,
        Sprite icon,
        Color iconColor)
    {
        if (popupPrefab == null)
        {
            Debug.LogError(
                "[NOTIFICATION] Brak popupPrefab.",
                this);

            return;
        }

        Transform parent =
            popupContainer != null
                ? popupContainer
                : transform;

        NotificationPopupUI popup =
            Instantiate(
                popupPrefab,
                parent);

        popup.Setup(
            itemName,
            tierText,
            icon,
            iconColor);
    }

    // =========================================================
    // TIER TEXT
    // =========================================================

    private string GetModuleTierText(
        ModuleTier tier)
    {
        switch (tier)
        {
            case ModuleTier.Tier1:
                return "1";

            case ModuleTier.Tier2:
                return "2";

            case ModuleTier.Tier3:
                return "3";

            default:
                return "?";
        }
    }

    private string GetResearchTierText(
        ResearchTier tier)
    {
        switch (tier)
        {
            case ResearchTier.Tier1:
                return "1";

            case ResearchTier.Tier2:
                return "2";

            case ResearchTier.Tier3:
                return "3";

            default:
                return "?";
        }
    }
}