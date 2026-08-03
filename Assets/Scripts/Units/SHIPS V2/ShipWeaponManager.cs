using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(ShipUnit))]
public class ShipWeaponManager : NetworkBehaviour
{
    private sealed class WeaponRuntime
    {
        public int SlotIndex;
        public FixedString64Bytes ModuleId;
        public ModuleDefinition Definition;

        public float NextAttackTime;

        public int CurrentAmmo;
        public bool IsReloading;
        public float ReloadEndTime;

        public int CurrentStacks;
        public float LastAttackTime;

        public ShipUnit CurrentTarget;
    }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs;

    private ShipUnit ship;

    private readonly List<WeaponRuntime> weapons = new();

    public int WeaponCount => weapons.Count;

    private void Awake()
    {
        ship = GetComponent<ShipUnit>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        UpdateWeaponTimers();
    }

    // Wywo³ywane przez ShipUnit po przypisaniu modu³ów podczas deployu.
    public void InitializeWeaponsFromShipModules()
    {
        if (!IsServer)
            return;

        weapons.Clear();

        if (ship == null)
            ship = GetComponent<ShipUnit>();

        if (ship == null)
        {
            Debug.LogError(
                $"[WEAPON MANAGER] Brak ShipUnit na {name}.",
                this);

            return;
        }

        AddWeaponFromSlot(
            0,
            ship.normalModule1.Value);

        AddWeaponFromSlot(
            1,
            ship.normalModule2.Value);

        AddWeaponFromSlot(
            2,
            ship.normalModule3.Value);

        AddWeaponFromSlot(
            3,
            ship.classModule.Value);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[WEAPON MANAGER] {ship.name} posiada " +
                $"{weapons.Count} aktywnych broni.",
                ship);
        }
    }

    private void AddWeaponFromSlot(
        int slotIndex,
        FixedString64Bytes moduleId)
    {
        if (moduleId.IsEmpty)
            return;

        if (ModuleDatabase.Instance == null)
        {
            Debug.LogError(
                "[WEAPON MANAGER] ModuleDatabase.Instance == null.",
                this);

            return;
        }

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId.ToString());

        if (module == null)
        {
            Debug.LogWarning(
                $"[WEAPON MANAGER] Nie znaleziono modu³u: {moduleId}",
                this);

            return;
        }

        if (!module.isWeapon)
            return;

        WeaponRuntime runtime = new()
        {
            SlotIndex = slotIndex,
            ModuleId = moduleId,
            Definition = module,

            // Pierwszy atak dopiero po pe³nym interwale.
            NextAttackTime =
                Time.time +
                GetFinalAttackInterval(module),

            CurrentAmmo =
                module.weaponHasMagazine
                    ? Mathf.Max(0, module.magazineCapacity)
                    : 0,

            IsReloading = false,
            ReloadEndTime = 0f,

            CurrentStacks = 0,
            LastAttackTime = 0f,

            CurrentTarget = null
        };

        weapons.Add(runtime);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[WEAPON MANAGER] Dodano broñ ze slotu {slotIndex}. " +
                $"moduleId={moduleId}, " +
                $"type={module.weaponType}, " +
                $"range={module.weaponRange:0.##}, " +
                $"interval={GetFinalAttackInterval(module):0.##}",
                ship);
        }
    }

    private void UpdateWeaponTimers()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntime weapon = weapons[i];

            UpdateReload(weapon);
            UpdateStackReset(weapon);
        }
    }

    private void UpdateReload(WeaponRuntime weapon)
    {
        if (!weapon.IsReloading)
            return;

        if (Time.time < weapon.ReloadEndTime)
            return;

        weapon.IsReloading = false;

        weapon.CurrentAmmo =
            Mathf.Max(
                0,
                weapon.Definition.magazineCapacity);

        weapon.NextAttackTime =
            Time.time +
            GetFinalAttackInterval(
                weapon.Definition);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[WEAPON MANAGER] Prze³adowano broñ " +
                $"{weapon.ModuleId}. Ammo={weapon.CurrentAmmo}",
                ship);
        }
    }

    private void UpdateStackReset(WeaponRuntime weapon)
    {
        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.weaponIsStacking)
            return;

        if (weapon.CurrentStacks <= 0)
            return;

        float resetTime =
            Mathf.Max(
                0f,
                definition.stackInactiveTimeToReset);

        if (Time.time <
            weapon.LastAttackTime + resetTime)
        {
            return;
        }

        weapon.CurrentStacks = 0;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[WEAPON MANAGER] Zresetowano stacki broni " +
                $"{weapon.ModuleId}.",
                ship);
        }
    }

    private float GetFinalAttackInterval(
        ModuleDefinition definition)
    {
        float baseInterval =
            Mathf.Max(
                0.01f,
                definition.weaponAttackInterval);

        float attackSpeedMultiplier =
            Mathf.Max(
                0.01f,
                ship.WeaponsAttackSpeedMultiplier);

        return baseInterval /
               attackSpeedMultiplier;
    }

    public float GetFinalHullDamage(
        ModuleDefinition definition)
    {
        if (definition == null)
            return 0f;

        return Mathf.Max(
            0f,
            definition.weaponHullDamage *
            ship.WeaponsDamageMultiplier);
    }

    public float GetFinalShieldDamage(
        ModuleDefinition definition)
    {
        if (definition == null)
            return 0f;

        return Mathf.Max(
            0f,
            definition.weaponShieldDamage *
            ship.WeaponsDamageMultiplier);
    }

    public bool HasAnyWeapon()
    {
        return weapons.Count > 0;
    }
}