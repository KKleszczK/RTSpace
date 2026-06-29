using UnityEngine;

[CreateAssetMenu(menuName = "RTS/Research Definition")]
public class ResearchDefinition : ScriptableObject
{
    public string researchId;
    public string displayName;

    [TextArea]
    public string description;

    public Sprite icon;
    public Sprite researchedIcon;

    public int baseMetalCost = 100;
    public int baseEnergyCost = 50;
    public float baseResearchTime = 10f;

    public float upgradeValue;
}