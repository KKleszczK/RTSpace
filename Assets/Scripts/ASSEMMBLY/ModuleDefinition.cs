using UnityEngine;

public enum ModuleTier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3
}

public enum ModuleType
{
    Miner,
    Fighter,
    Utility
}

public enum AuraEffectType
{
    RangeBoost,
    Healing
}

public enum BaseBoosterEffectType
{
    AssemblySpeed,
    EnergyProduction,
    LabSpeed
}

public enum WeaponType
{
    Laser,
    Projectile,
    Aura
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
    public ModuleTier tier;
    public ModuleType type;

    [Header("Crafting")]
    public int metalCost = 100;
    public int energyCost = 50;
    public float craftTime = 10f;

    [Header("Restrictions")]
    [Min(1)]
    public int maxCopiesPerPlayer = 1;

    [Header("Stat Modifiers")]
    public float shieldFlat;
    public float shieldPercent;

    public float hpFlat;
    public float hpPercent;

    public float moveSpeedFlat;
    public float moveSpeedPercent;

    public float allWeaponsDamagePercent;
    public float allWeaponsAttackSpeedPercent;

    [Header("Aura")]
    public bool haveAura;
    public float auraRange = 5f;
    public AuraEffectType auraEffect;
    public float auraEffectValue;

    [Header("Base Booster")]
    public bool haveBaseBooster;
    public BaseBoosterEffectType baseBoosterEffect;
    public float baseBoosterValue;

    [Header("Mining")]
    public bool isSocketMiner;
    public float mineInterval = 1f;
    public float miningAmountAtFullDensity = 1f;
    public float densityRemovedPerHit = 1f;
    public int miningModuleDurability;

    [Header("Weapon")]
    public bool isWeapon;
    public WeaponType weaponType;

    public float weaponRange = 5f;
    public float weaponAttackInterval = 1f;

    public float weaponHullDamage = 10f;
    public float weaponShieldDamage = 10f;

    public float projectileSpeed = 10f;

    [Header("Magazine")]
    public bool weaponHasMagazine;
    public int magazineCapacity = 10;
    public bool magazineCanReload = true;
    public float magazineReloadTime = 3f;

    [Header("AOE")]
    public bool weaponHasAoe;
    public float weaponAoeRange = 2f;
    public float weaponAoeDamageMultiplier = 1f;

    [Header("Stacking")]
    public bool weaponIsStacking;
    public int weaponMaxStacks = 5;
    public float stackMovementSpeedPercent;
    public float stackDamagePercent;
    public float stackRangePercent;
    public float stackAttackSpeedPercent;
    public float stackInactiveTimeToReset = 3f;

    [Header("Multiple Targets")]
    public int maxTargets = 1;

    public bool canChainAttack;
    public int chainJumps;
    public float chainDamageMultiplier = 0.5f;

    [Header("Slow")]
    public bool canSlowOnHit;
    public float slowPercent = 20f;
    public float slowDuration = 2f;

    [Header("Self Damage")]
    public bool selfHarmOnAttack;
    public float selfDamage;
}