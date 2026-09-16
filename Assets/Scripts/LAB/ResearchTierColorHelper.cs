using UnityEngine;
using UnityEngine.UI;

public static class ResearchTierColorHelper
{
    // =========================================================
    // GET COLOR
    // =========================================================

    public static Color GetColor(
        ResearchTier tier,
        bool locked = false)
    {
        if (ResearchTierColors.Instance == null)
        {
            return locked
                ? Color.gray
                : Color.white;
        }

        return ResearchTierColors.Instance.GetColor(
            tier,
            locked);
    }


    // =========================================================
    // APPLY
    // =========================================================

    public static void ApplyToImage(
        Image image,
        ResearchDefinition research,
        bool locked = false)
    {
        if (image == null)
            return;

        if (research == null)
        {
            image.sprite =
                null;

            image.color =
                Color.white;

            return;
        }

        image.sprite =
            research.icon;

        image.color =
            GetColor(
                research.tier,
                locked);
    }
}