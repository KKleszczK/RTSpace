using UnityEngine;
using UnityEngine.UI;

public static class ModuleTierColorHelper
{
    public static Color GetColor(
        ModuleTier tier,
        bool locked)
    {
        if (ModuleTierColors.Instance == null)
            return Color.white;

        return ModuleTierColors.Instance.GetColor(
            tier,
            locked);
    }

    public static void ApplyToImage(
        Image image,
        ModuleDefinition module,
        bool locked)
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

        image.color =
            GetColor(
                module.tier,
                locked);
    }

    public static void ApplyToImage(
    Image image,
    ModuleDefinition module)
    {
        ApplyToImage(
            image,
            module,
            false);
    }

    public static Color GetColor(
    ModuleTier tier)
    {
        return GetColor(
            tier,
            false);
    }
}