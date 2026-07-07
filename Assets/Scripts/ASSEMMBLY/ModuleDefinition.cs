using UnityEngine;

public enum ModuleType
{
    Fighter,
    Utility,
    Miner
}

public enum ModuleTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}

[CreateAssetMenu(fileName = "New Module", menuName = "RTS/Module")]
public class ModuleDefinition : ScriptableObject
{
    [Header("General")]
    public string moduleId;
    public string displayName;

    [TextArea(3, 6)]
    public string description;

    public Sprite icon;

    public ModuleType moduleType;
    public ModuleTier moduleTier;

    public int maxStack = 1;

    [Header("Crafting")]
    public int metalCost;
    public int energyCost;
    public float craftTime;

    [Header("Stats")]
    public int hpBonus;
    public int damageBonus;
    public float attackSpeedBonus;
    public float moveSpeedBonus;
    public float miningSpeedBonus;

    [Header("Abilities")]
    public bool unlockAbility;
    public string abilityId;
}