using System.Collections.Generic;
using UnityEngine;

public enum ResearchEffectType
{
    ShipSpeed,
    ShipHp,
    ShipShield,

    StationResearchSpeed,
    StationHullHp,
    StationShield,

    GeneratorBoost,

    UnlockModules
}

public enum ResearchValueType
{
    Flat,
    Percent
}

public enum ResearchTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}

[CreateAssetMenu(
    fileName = "New Research",
    menuName = "RTS/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    // =========================================================
    // GENERAL
    // =========================================================

    [Header("General")]
    public string researchId;
    public string displayName;

    [TextArea(3, 6)]
    public string description;

    public Sprite icon;
    public Sprite researchedIcon;

    public ResearchTier tier;

    // =========================================================
    // COST
    // =========================================================

    [Header("Cost")]
    [Min(0)]
    public int baseMetalCost = 100;

    [Min(0)]
    public int baseEnergyCost = 50;

    [Min(0.01f)]
    public float baseResearchTime = 10f;

    // =========================================================
    // EFFECT
    // =========================================================

    [Header("Effect")]
    public ResearchEffectType effectType;

    public ResearchValueType valueType;

    public float value;

    // =========================================================
    // MODULE UNLOCK
    // =========================================================

    [Header("Module Unlock")]
    public List<string> unlockedModuleIds = new();
}