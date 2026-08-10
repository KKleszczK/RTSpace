using UnityEngine;
using UnityEngine.UI;

public static class ModuleTierColorHelper
{
    public static Color GetColor(ModuleTier tier)
    {
        switch (tier)
        {
            case ModuleTier.Tier1:
                return new Color(
                    36f/255f,
                    149f/255f,
                    40f/255f,
                    1f); ;

            case ModuleTier.Tier2:
                return new Color(
                    0f / 255f,
                    115f / 255f,
                    223f / 255f,
                    1f); ;

            case ModuleTier.Tier3:
                return new Color(
                    223f / 255f,
                    22f / 255f,
                    46f / 255f,
                    1f); ;

            default:
                return Color.white;
        }
    }

    public static void ApplyToImage(
        Image image,
        ModuleDefinition module)
    {
        if (image == null)
            return;

        if (module == null)
        {
            image.sprite = null;
            image.color = Color.white;
            return;
        }

        image.sprite = module.icon;
        image.color = GetColor(module.tier);
    }
}