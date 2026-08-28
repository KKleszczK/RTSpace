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

    // =========================================================
    // HULL REPAIR
    // =========================================================

    [Header("Hull Repair")]
    [SerializeField]
    private bool hullRepairEnabled = true;

    [SerializeField, Min(0.05f)]
    private float hullRepairInterval = 1f;

    [SerializeField, Min(0)]
    private int hullRepairAmount = 5;

    [SerializeField, Min(0)]
    private int hullRepairMetalCost = 2;

    private float nextHullRepairTime;

    // =========================================================
    // SHIELD RECHARGE
    // =========================================================

    [Header("Shield Recharge")]
    [SerializeField]
    private bool shieldRechargeEnabled = true;

    [SerializeField, Min(0.05f)]
    private float shieldRechargeInterval = 1f;

    [SerializeField, Min(0)]
    private int shieldRechargeAmount = 5;

    [SerializeField, Min(0)]
    private int shieldRechargeEnergyCost = 2;

    private float nextShieldRechargeTime;

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
        if (owner == null)
        {
            owner =
                GetComponent<UnitOwner>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
            return;

        FindPlayerResources();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!IsServer)
            return;

        if (owner == null)
            return;

        if (resources == null)
        {
            FindPlayerResources();
        }

        // =====================================================
        // SAFE ZONE / BOOSTERS
        // =====================================================

        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime =
                Time.time + updateInterval;

            RefreshSafeZone();
        }

        // =====================================================
        // HULL REPAIR
        // =====================================================

        if (hullRepairEnabled &&
            Time.time >= nextHullRepairTime)
        {
            nextHullRepairTime =
                Time.time +
                hullRepairInterval;

            ProcessHullRepair();
        }

        // =====================================================
        // SHIELD RECHARGE
        // =====================================================

        if (shieldRechargeEnabled &&
            Time.time >= nextShieldRechargeTime)
        {
            nextShieldRechargeTime =
                Time.time +
                shieldRechargeInterval;

            ProcessShieldRecharge();
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

    private bool IsValidShipInSafeZone(
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
    // HULL REPAIR
    // =========================================================

    private void ProcessHullRepair()
    {
        if (resources == null)
            return;

        if (hullRepairAmount <= 0)
            return;

        foreach (ShipUnit ship in shipsInSafeZone)
        {
            if (!IsValidShipInSafeZone(ship))
                continue;

            // Pe³ne HP - nic nie robimy
            // i nie pobieramy metalu.
            if (ship.hp.Value >=
                ship.MaxHp)
            {
                continue;
            }

            int metalCost =
                Mathf.Max(
                    0,
                    hullRepairMetalCost);

            if (!resources.CanAfford(
                    metalCost,
                    0))
            {
                continue;
            }

            resources.Spend(
                metalCost,
                0);

            ship.Heal(
                hullRepairAmount);
        }
    }

    // =========================================================
    // SHIELD RECHARGE
    // =========================================================

    private void ProcessShieldRecharge()
    {
        if (resources == null)
            return;

        if (shieldRechargeAmount <= 0)
            return;

        foreach (ShipUnit ship in shipsInSafeZone)
        {
            if (!IsValidShipInSafeZone(ship))
                continue;

            // Ten statek w ogóle nie posiada tarczy.
            if (ship.MaxShield <= 0)
                continue;

            // Pe³na tarcza - nic nie robimy
            // i nie pobieramy energii.
            if (ship.shield.Value >=
                ship.MaxShield)
            {
                continue;
            }

            int energyCost =
                Mathf.Max(
                    0,
                    shieldRechargeEnergyCost);

            if (!resources.CanAfford(
                    0,
                    energyCost))
            {
                continue;
            }

            resources.Spend(
                0,
                energyCost);

            ship.RestoreShield(
                shieldRechargeAmount);
        }
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