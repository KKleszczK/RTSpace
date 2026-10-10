using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseSafeZone : NetworkBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private UnitOwner owner;

    private PlayerResources resources;

    // =========================================================
    // SAFE ZONE
    // =========================================================

    [Header("Safe Zone")]
    [SerializeField] private float safeZoneRange = 10f;

    [SerializeField, Min(0.05f)]
    private float updateInterval = 0.5f;

    private float nextUpdateTime;

    private readonly List<ShipUnit>
        shipsInSafeZone = new();

    enum CostType
    {
    Energy,
    Metal
    }



    // =========================================================
    // RegenResourcees
    // =========================================================
    [Header("Regen Cost Types")]
    [SerializeField] private CostType HullRegenCostType = CostType.Metal;
    [SerializeField] private CostType ShieldRegenCostType = CostType.Energy;

    [SerializeField] private BaseCore core;
    [SerializeField] private BaseUnit baseUnit;
    private float nextShipRegenTime;
    private float nextBaseRegenTime;
    private int lastCoreTier = -1;



    // =========================================================
    // Ships Tier 1
    // =========================================================
    [Header("Ships Regen - Core Level 1")]
    [SerializeField] private float RegenInterval1 = 1.0f;
    [SerializeField] private float HullRegenFlat1 = 0.0f;
    [SerializeField] private float HullRegenFlatCost1 = 0.0f;
    [SerializeField] private float HullRegenProcent1 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float HullRegenProcentRatioCost1 = 0.0f;

    [SerializeField] private float ShieldRegenFlat1 = 0.0f;
    [SerializeField] private float ShieldRegenFlatCost1 = 0.0f;
    [SerializeField] private float ShieldRegenProcent1 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float ShieldRegenProcentRatioCost1 = 0.0f;



    // =========================================================
    // Ships Tier 2
    // =========================================================
    [Header("Ships Regen - Core Level 2")]
    [SerializeField] private float RegenInterval2 = 1.0f;
    [SerializeField] private float HullRegenFlat2 = 0.0f;
    [SerializeField] private float HullRegenFlatCost2 = 0.0f;
    [SerializeField] private float HullRegenProcent2 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float HullRegenProcentRatioCost2 = 0.0f;

    [SerializeField] private float ShieldRegenFlat2 = 0.0f;
    [SerializeField] private float ShieldRegenFlatCost2 = 0.0f;
    [SerializeField] private float ShieldRegenProcent2 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float ShieldRegenProcentRatioCost2 = 0.0f;



    // =========================================================
    // Ships Tier 3
    // =========================================================
    [Header("Ships Regen - Core Level 3")]
    [SerializeField] private float RegenInterval3 = 1.0f;
    [SerializeField] private float HullRegenFlat3 = 0.0f;
    [SerializeField] private float HullRegenFlatCost3 = 0.0f;
    [SerializeField] private float HullRegenProcent3 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float HullRegenProcentRatioCost3 = 0.0f;

    [SerializeField] private float ShieldRegenFlat3 = 0.0f;
    [SerializeField] private float ShieldRegenFlatCost3 = 0.0f;
    [SerializeField] private float ShieldRegenProcent3 = 0.0f;
    [SerializeField]
    [Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float ShieldRegenProcentRatioCost3 = 0.0f;



    // =========================================================
    // Base Self Regen Tier 1
    // =========================================================
    [Header("Base Self Regen - Core Level 1")]
    [SerializeField] private float BaseRegenInterval1 = 1.0f;

    [SerializeField] private float BaseHullRegenFlat1 = 0.0f;
    [SerializeField] private float BaseHullRegenFlatCost1 = 0.0f;
    [SerializeField] private float BaseHullRegenProcent1 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseHullRegenProcentRatioCost1 = 0.0f;

    [SerializeField] private float BaseShieldRegenFlat1 = 0.0f;
    [SerializeField] private float BaseShieldRegenFlatCost1 = 0.0f;
    [SerializeField] private float BaseShieldRegenProcent1 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseShieldRegenProcentRatioCost1 = 0.0f;



    // =========================================================
    // Base Self Regen Tier 2
    // =========================================================
    [Header("Base Self Regen - Core Level 2")]
    [SerializeField] private float BaseRegenInterval2 = 1.0f;

    [SerializeField] private float BaseHullRegenFlat2 = 0.0f;
    [SerializeField] private float BaseHullRegenFlatCost2 = 0.0f;
    [SerializeField] private float BaseHullRegenProcent2 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseHullRegenProcentRatioCost2 = 0.0f;

    [SerializeField] private float BaseShieldRegenFlat2 = 0.0f;
    [SerializeField] private float BaseShieldRegenFlatCost2 = 0.0f;
    [SerializeField] private float BaseShieldRegenProcent2 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseShieldRegenProcentRatioCost2 = 0.0f;



    // =========================================================
    // Base Self Regen Tier 3
    // =========================================================
    [Header("Base Self Regen - Core Level 3")]
    [SerializeField] private float BaseRegenInterval3 = 1.0f;

    [SerializeField] private float BaseHullRegenFlat3 = 0.0f;
    [SerializeField] private float BaseHullRegenFlatCost3 = 0.0f;
    [SerializeField] private float BaseHullRegenProcent3 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseHullRegenProcentRatioCost3 = 0.0f;

    [SerializeField] private float BaseShieldRegenFlat3 = 0.0f;
    [SerializeField] private float BaseShieldRegenFlatCost3 = 0.0f;
    [SerializeField] private float BaseShieldRegenProcent3 = 0.0f;
    [SerializeField, Tooltip("1 = 1:1, 10 = 1:10 (100 hp cost 10), 0 = no ratio cost")]
    private float BaseShieldRegenProcentRatioCost3 = 0.0f;









    
    // =========================================================
    // CURRENT BOOSTERS
    // =========================================================

    public NetworkVariable<float> AssemblySpeedBonusPercent =
    new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> EnergyProductionBonusPercent =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<float> LabSpeedBonusPercent =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public NetworkVariable<float> DensityBoostPercent =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);



    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        if (owner == null) owner = GetComponent<UnitOwner>();
        if (core == null) core = GetComponent<BaseCore>();
        if (baseUnit == null) baseUnit = GetComponent<BaseUnit>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        FindPlayerResources();
        lastCoreTier = GetCoreTier();
        nextShipRegenTime = Time.time + GetShipInterval(lastCoreTier);
        nextBaseRegenTime = Time.time + GetBaseInterval(lastCoreTier);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsServer || owner == null) return;
        if (resources == null) FindPlayerResources();

        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + Mathf.Max(0.05f, updateInterval);
            RefreshSafeZone();
        }

        int tier = GetCoreTier();
        if (tier != lastCoreTier)
        {
            lastCoreTier = tier;
            nextShipRegenTime = Time.time + GetShipInterval(tier);
            nextBaseRegenTime = Time.time + GetBaseInterval(tier);
        }

        if (Time.time >= nextShipRegenTime)
        {
            nextShipRegenTime = Time.time + GetShipInterval(tier);
            ProcessShipRegen(tier);
        }

        if (Time.time >= nextBaseRegenTime)
        {
            nextBaseRegenTime = Time.time + GetBaseInterval(tier);
            ProcessBaseRegen(tier);
        }
    }

    // =========================================================
    // SAFE ZONE
    // =========================================================

    private void RefreshSafeZone()
    {
        shipsInSafeZone.Clear();

        AssemblySpeedBonusPercent.Value = 0f;
        EnergyProductionBonusPercent.Value = 0f;
        LabSpeedBonusPercent.Value = 0f;
        DensityBoostPercent.Value = 0f;

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (!IsValidShipInSafeZone(ship))
                continue;

            shipsInSafeZone.Add(ship);

            CheckModule(
                ship,
                0,
                ship.normalModule1.Value);

            CheckModule(
                ship,
                1,
                ship.normalModule2.Value);

            CheckModule(
                ship,
                2,
                ship.normalModule3.Value);

            CheckModule(
                ship,
                3,
                ship.classModule.Value);
        }
    }

    public bool IsValidShipInSafeZone(
        ShipUnit ship)
    {
        if (ship == null)
            return false;

        if (!ship.IsSpawned)
            return false;

        if (ship.isDead.Value)
            return false;

        if (ship.ownerId.Value !=
            owner.ownerId.Value)
        {
            return false;
        }

        Vector3 offset =
            ship.transform.position -
            transform.position;

        offset.y = 0f;

        float range =
            Mathf.Max(
                0f,
                safeZoneRange);

        return offset.sqrMagnitude <=
               range * range;
    }

    // =========================================================
    // REGENERATION
    // =========================================================

    private struct RegenSettings
    {
        public float flat, flatCost, percent, ratio;
        public RegenSettings(float flat, float flatCost, float percent, float ratio)
        {
            this.flat = flat;
            this.flatCost = flatCost;
            this.percent = percent;
            this.ratio = ratio;
        }
    }

    private int GetCoreTier() => core != null ? Mathf.Clamp(core.tier.Value, 1, 3) : 1;

    private float GetShipInterval(int tier) => Mathf.Max(0.05f, tier switch
    {
        2 => RegenInterval2,
        3 => RegenInterval3,
        _ => RegenInterval1
    });

    private float GetBaseInterval(int tier) => Mathf.Max(0.05f, tier switch
    {
        2 => BaseRegenInterval2,
        3 => BaseRegenInterval3,
        _ => BaseRegenInterval1
    });

    private RegenSettings GetSettings(int tier, bool self, bool shield)
    {
        if (self)
        {
            if (shield) return tier switch
            {
                2 => new(BaseShieldRegenFlat2, BaseShieldRegenFlatCost2, BaseShieldRegenProcent2, BaseShieldRegenProcentRatioCost2),
                3 => new(BaseShieldRegenFlat3, BaseShieldRegenFlatCost3, BaseShieldRegenProcent3, BaseShieldRegenProcentRatioCost3),
                _ => new(BaseShieldRegenFlat1, BaseShieldRegenFlatCost1, BaseShieldRegenProcent1, BaseShieldRegenProcentRatioCost1)
            };
            return tier switch
            {
                2 => new(BaseHullRegenFlat2, BaseHullRegenFlatCost2, BaseHullRegenProcent2, BaseHullRegenProcentRatioCost2),
                3 => new(BaseHullRegenFlat3, BaseHullRegenFlatCost3, BaseHullRegenProcent3, BaseHullRegenProcentRatioCost3),
                _ => new(BaseHullRegenFlat1, BaseHullRegenFlatCost1, BaseHullRegenProcent1, BaseHullRegenProcentRatioCost1)
            };
        }
        if (shield) return tier switch
        {
            2 => new(ShieldRegenFlat2, ShieldRegenFlatCost2, ShieldRegenProcent2, ShieldRegenProcentRatioCost2),
            3 => new(ShieldRegenFlat3, ShieldRegenFlatCost3, ShieldRegenProcent3, ShieldRegenProcentRatioCost3),
            _ => new(ShieldRegenFlat1, ShieldRegenFlatCost1, ShieldRegenProcent1, ShieldRegenProcentRatioCost1)
        };
        return tier switch
        {
            2 => new(HullRegenFlat2, HullRegenFlatCost2, HullRegenProcent2, HullRegenProcentRatioCost2),
            3 => new(HullRegenFlat3, HullRegenFlatCost3, HullRegenProcent3, HullRegenProcentRatioCost3),
            _ => new(HullRegenFlat1, HullRegenFlatCost1, HullRegenProcent1, HullRegenProcentRatioCost1)
        };
    }

    // Cost is flat cost + (actual percentage healing / ratio).
    // Flat healing has no proportional cost. Integer HP/shield and resources are rounded up.
    private void ApplyRegen(int current, int maximum, RegenSettings settings, CostType costType, System.Action<float> apply)
    {
        if (resources == null || maximum <= 0 || current >= maximum) return;

        int flatHeal = Mathf.Max(0, Mathf.CeilToInt(settings.flat));
        int percentHeal = Mathf.Max(0, Mathf.CeilToInt(maximum * Mathf.Max(0f, settings.percent) / 100f));
        int missing = maximum - current;
        int actualFlat = Mathf.Min(missing, flatHeal);
        int actualPercent = Mathf.Min(missing - actualFlat, percentHeal);
        int total = actualFlat + actualPercent;
        if (total <= 0) return;

        float variableCost = settings.ratio > 0f ? actualPercent / settings.ratio : 0f;
        int cost = Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0f, settings.flatCost) + variableCost));
        int metalCost = costType == CostType.Metal ? cost : 0;
        int energyCost = costType == CostType.Energy ? cost : 0;
        if (!resources.CanAfford(metalCost, energyCost)) return;

        resources.Spend(metalCost, energyCost);
        apply(total);
    }

    private void ProcessShipRegen(int tier)
    {
        if (resources == null) return;
        RegenSettings hull = GetSettings(tier, false, false);
        RegenSettings shield = GetSettings(tier, false, true);
        foreach (ShipUnit ship in shipsInSafeZone)
        {
            if (!IsValidShipInSafeZone(ship)) continue;
            ApplyRegen(ship.hp.Value, ship.MaxHp, hull, HullRegenCostType, ship.Heal);
            ApplyRegen(ship.shield.Value, ship.MaxShield, shield, ShieldRegenCostType, ship.RestoreShield);
        }
    }

    private void ProcessBaseRegen(int tier)
    {
        if (resources == null || baseUnit == null || !baseUnit.IsSpawned || baseUnit.isDead.Value) return;
        ApplyRegen(baseUnit.hp.Value, baseUnit.maxHp.Value,
            GetSettings(tier, true, false), HullRegenCostType, baseUnit.Heal);
        ApplyRegen(baseUnit.shield.Value, baseUnit.maxShield.Value,
            GetSettings(tier, true, true), ShieldRegenCostType, baseUnit.RestoreShield);
    }

    // =========================================================
    // MODULE BOOSTERS
    // =========================================================

    private void CheckModule(
        ShipUnit ship,
        int slotIndex,
        FixedString64Bytes moduleId)
    {
        if (ship == null)
            return;

        if (moduleId.IsEmpty)
            return;

        if (ModuleDatabase.Instance == null)
            return;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
            return;

        if (!module.haveBaseBooster)
            return;

        float moduleMultiplier =
            ship.GetModuleEffectMultiplier(
                slotIndex,
                module);

        float finalValue =
            module.baseBoosterValue *
            moduleMultiplier;

        AddBoosterValue(
            module.baseBoosterEffect,
            finalValue);
    }

    private void AddBoosterValue(
    BaseBoosterEffectType effect,
    float value)
    {
        switch (effect)
        {
            case BaseBoosterEffectType.AssemblySpeed:

                AssemblySpeedBonusPercent.Value +=
                    value;

                break;

            case BaseBoosterEffectType.EnergyProduction:

                EnergyProductionBonusPercent.Value +=
                    value;

                break;

            case BaseBoosterEffectType.LabSpeed:

                LabSpeedBonusPercent.Value =
                    Mathf.Max(
                        LabSpeedBonusPercent.Value,
                        value);

                break;

            case BaseBoosterEffectType.DencityBoost:

                DensityBoostPercent.Value +=
                    value;

                break;
        }
    }

    // =========================================================
    // PLAYER RESOURCES
    // =========================================================

    private void FindPlayerResources()
    {
        if (owner == null)
            return;

        PlayerResources[] all =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources candidate in all)
        {
            if (!candidate.IsSpawned)
                continue;

            if (candidate.OwnerClientId !=
                owner.ownerId.Value)
            {
                continue;
            }

            resources = candidate;
            return;
        }
    }

    // =========================================================
    // PUBLIC BOOSTER VALUES
    // =========================================================

    public float GetAssemblySpeedMultiplier()
    {
        return Mathf.Max(
            0.01f,
            1f +
            AssemblySpeedBonusPercent.Value / 100f);
    }

    public float GetEnergyProductionMultiplier()
    {
        return Mathf.Max(
            0.01f,
            1f +
            EnergyProductionBonusPercent.Value / 100f);
    }

    public float GetLabSpeedMultiplier()
    {
        return Mathf.Max(
            0.01f,
            1f +
            LabSpeedBonusPercent.Value / 100f);
    }

    public float GetDensityBoostMultiplier()
    {
        return Mathf.Max(
            0.01f,
            1f +
            DensityBoostPercent.Value / 100f);
    }

    public float GetAssemblySpeedBonusPercent()
    {
        return Mathf.Clamp(
            AssemblySpeedBonusPercent.Value,
            0f,
            100f);
    }

    public float GetLabSpeedBonusPercent()
    {
        return Mathf.Clamp(
            LabSpeedBonusPercent.Value,
            0f,
            100f);
    }
}