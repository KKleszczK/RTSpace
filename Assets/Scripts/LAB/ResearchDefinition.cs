using UnityEngine;

public enum ResearchType
{
    OneTime,
    Repeatable
}

[CreateAssetMenu(menuName = "RTS/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    public string researchId;
    public string displayName;

    [TextArea]
    public string description;

    public Sprite icon;

    public ResearchType researchType;

    public int baseMetalCost = 100;
    public int baseEnergyCost = 50;
    public float baseResearchTime = 10f;

    public float repeatCostMultiplier = 1.5f;

    public float valuePerLevel = 0.2f;
}