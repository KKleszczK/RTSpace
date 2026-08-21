using UnityEngine;
using UnityEngine.UI;

public static class ResearchTierColorHelper
{
    public static Color GetColor(
        ResearchTier tier)
    {
        switch (tier)
        {
            case ResearchTier.Tier1:
                return new Color(
                    36f / 255f,
                    149f / 255f,
                    40f / 255f,
                    1f);

            case ResearchTier.Tier2:
                return new Color(
                    0f / 255f,
                    115f / 255f,
                    223f / 255f,
                    1f);

            case ResearchTier.Tier3:
                return new Color(
                    223f / 255f,
                    22f / 255f,
                    46f / 255f,
                    1f);

            default:
                return Color.white;
        }
    }

    public static void ApplyToImage(
        Image image,
        ResearchDefinition research)
    {
        if (image == null)
            return;

        if (research == null)
        {
            image.sprite = null;
            image.color = Color.white;
            return;
        }

        image.sprite =
            research.icon;

        image.color =
            GetColor(
                research.tier);
    }
}