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

        public bool IsDisabled;

        public int CurrentStacks;
        public float LastAttackTime;

        public IDamageable CurrentTarget;
    }

    [Header("Laser Visuals")]
    [SerializeField] private Transform[] laserOrigins = new Transform[4];
    [SerializeField] private LineRenderer[] laserLines = new LineRenderer[4];

    [Header("AOE Visuals")]
    [SerializeField] private GameObject aoeImpactPrefab;

    [Header("Chain Visual")]
    [SerializeField] private LineRenderer chainLinePrefab;
    [SerializeField] private float chainVisualLifetime = 0.2f;

    private sealed class LaserVisual
    {
        public LineRenderer Line;
        public IDamageable Target;
    }

    private readonly Dictionary<int, LaserVisual> laserVisuals = new();

    [Header("Projectile")]
    [SerializeField] private NetworkObject projectilePrefab;
    [SerializeField] private Transform[] projectileOrigins = new Transform[4];



    [Header("Debug")]
    [SerializeField] private bool showDebugLogs;

    private ShipUnit ship;

    private readonly List<WeaponRuntime> weapons = new();

    public int WeaponCount => weapons.Count;

    private void Awake()
    {
        ship = GetComponent<ShipUnit>();

        InitializeLaserVisualsLocal();
    }

    private void Update()
    {
        UpdateLaserVisuals();

        if (!IsServer)
            return;

        UpdateWeaponTimers();
        UpdateMagazineReloads();

        UpdateAuraWeapons();
        UpdateLaserWeapons();
        UpdateProjectileWeapons();
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
            ? Mathf.Max(1, module.magazineCapacity)
            : 0,

            IsReloading = false,
            ReloadEndTime = 0f,

            IsDisabled = false,

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

            
            UpdateStackReset(weapon);
        }
    }

    private void UpdateStackReset(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

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

        RecalculateStackMovementSpeed();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[STACK] Zresetowano stacki " +
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

    private float GetStackDamageMultiplier(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null ||
            !weapon.Definition.weaponIsStacking)
        {
            return 1f;
        }

        float percent =
            weapon.Definition.stackDamagePercent *
            weapon.CurrentStacks;

        return Mathf.Max(
            0f,
            1f + percent / 100f);
    }

    private float GetFinalHullDamage(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return 0f;
        }

        float classSlotMultiplier =
            ship.GetModuleEffectMultiplier(
                weapon.SlotIndex,
                weapon.Definition);

        float baseDamage =
            weapon.Definition.weaponHullDamage *
            classSlotMultiplier *
            ship.WeaponsDamageMultiplier;

        return Mathf.Max(
            0f,
            baseDamage *
            GetStackDamageMultiplier(weapon));
    }

    private float GetFinalShieldDamage(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return 0f;
        }

        float classSlotMultiplier =
            ship.GetModuleEffectMultiplier(
                weapon.SlotIndex,
                weapon.Definition);

        float baseDamage =
            weapon.Definition.weaponShieldDamage *
            classSlotMultiplier *
            ship.WeaponsDamageMultiplier;

        return Mathf.Max(
            0f,
            baseDamage *
            GetStackDamageMultiplier(weapon));
    }

    public bool HasAnyWeapon()
    {
        return weapons.Count > 0;
    }

    private float GetFinalWeaponRange(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return 0f;
        }

        float multiplier = 1f;

        if (weapon.Definition.weaponIsStacking)
        {
            float percent =
                weapon.Definition.stackRangePercent *
                weapon.CurrentStacks;

            multiplier =
                Mathf.Max(
                    0f,
                    1f + percent / 100f);
        }

        return Mathf.Max(
            0f,
            weapon.Definition.weaponRange *
            multiplier *
            ship.AuraRangeMultiplier);
    }

    private float GetFinalAttackInterval(
    WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return 1f;
        }

        float baseInterval =
            Mathf.Max(
                0.01f,
                weapon.Definition.weaponAttackInterval);

        float shipAttackSpeedMultiplier =
            Mathf.Max(
                0.01f,
                ship.WeaponsAttackSpeedMultiplier);

        float stackMultiplier = 1f;

        if (weapon.Definition.weaponIsStacking)
        {
            float percent =
                weapon.Definition.stackAttackSpeedPercent *
                weapon.CurrentStacks;

            stackMultiplier =
                Mathf.Max(
                    0.01f,
                    1f + percent / 100f);
        }

        return baseInterval /
               shipAttackSpeedMultiplier /
               stackMultiplier;
    }

    private void RecalculateStackMovementSpeed()
    {
        float totalPercent = 0f;

        foreach (WeaponRuntime weapon in weapons)
        {
            if (weapon == null ||
                weapon.Definition == null ||
                !weapon.Definition.weaponIsStacking)
            {
                continue;
            }

            totalPercent +=
                weapon.Definition.stackMovementSpeedPercent *
                weapon.CurrentStacks;
        }

        ship.SetWeaponStackMovementPercent(
            totalPercent);
    }


    // =========================================================
    // AURA
    // =========================================================

    private void UpdateAuraWeapons()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntime weapon = weapons[i];

            if (weapon.Definition == null)
                continue;

            if (weapon.Definition.weaponType != WeaponType.Aura)
                continue;

            if (!CanWeaponAttack(weapon))
                continue;

            int targetsHit =
                ExecuteAuraAttack(weapon);

            /*
             * Brak celów:
             * nie wykonano ataku,
             * nie ma self damage,
             * nie rozpoczynamy cooldownu.
             */
            if (targetsHit <= 0)
                continue;

            /*
             * Atak zosta³ ju¿ wykonany na wszystkich celach.
             * Dopiero teraz nak³adamy self damage jeden raz,
             * niezale¿nie od liczby trafionych statków.
             */
            ApplySelfDamageAfterAttack(weapon);
            ConsumeAmmoAfterAttack(weapon);
            AddStackAfterAttack(weapon);

            if (!weapon.IsReloading &&
                !weapon.IsDisabled)
            {
                weapon.NextAttackTime =
                    Time.time +
                    GetFinalAttackInterval(
                        weapon);
            }
        }
    }

    private int ExecuteAuraAttack(
    WeaponRuntime weapon)
    {
        if (ship == null ||
            weapon == null ||
            weapon.Definition == null)
        {
            return 0;
        }

        float range =
            GetFinalWeaponRange(weapon);

        float hullDamage =
            GetFinalHullDamage(
                weapon);

        float shieldDamage =
            GetFinalShieldDamage(
                weapon);

        int targetsHit = 0;

        // =========================================================
        // SHIPS
        // =========================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit target in allShips)
        {
            if (TryExecuteAuraOnTarget(
                    weapon,
                    target,
                    range,
                    hullDamage,
                    shieldDamage))
            {
                targetsHit++;
            }
        }

        // =========================================================
        // BASES
        // =========================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit target in allBases)
        {
            if (TryExecuteAuraOnTarget(
                    weapon,
                    target,
                    range,
                    hullDamage,
                    shieldDamage))
            {
                targetsHit++;
            }
        }

        if (showDebugLogs &&
            targetsHit > 0)
        {
            Debug.Log(
                $"[AURA] {ship.name} wykona³ atak. " +
                $"Range={range:0.##}, " +
                $"HullDamage={hullDamage:0.##}, " +
                $"ShieldDamage={shieldDamage:0.##}, " +
                $"Targets={targetsHit}",
                ship);
        }

        return targetsHit;
    }

    private bool TryExecuteAuraOnTarget(
    WeaponRuntime weapon,
    IDamageable target,
    float range,
    float hullDamage,
    float shieldDamage)
    {
        if (!IsValidAuraTarget(
                target,
                range))
        {
            return false;
        }

        target.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        ApplySlowOnHit(
            weapon,
            target);

        ApplyAoeDamage(
            weapon,
            target,
            target.DamageTransform.position);

        ApplyChainAttack(
            weapon,
            target,
            hullDamage,
            shieldDamage);

        return true;
    }


    private bool IsValidAuraTarget(
    IDamageable target,
    float range)
    {
        return IsValidDamageTarget(
            target,
            range);
    }
    // =========================================================
    // LASER
    // =========================================================

    private void UpdateLaserWeapons()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntime weapon = weapons[i];

            if (weapon.Definition == null)
                continue;

            if (weapon.Definition.weaponType != WeaponType.Laser)
                continue;

            UpdateLaserWeapon(weapon);
        }
    }

    private void UpdateLaserWeapon(
    WeaponRuntime weapon)
    {
        float range =
            GetFinalWeaponRange(weapon);

        if (!IsValidLaserTarget(
                weapon.CurrentTarget,
                range))
        {
            weapon.CurrentTarget =
                FindNearestEnemy(range);

            if (weapon.CurrentTarget != null)
            {
                NetworkBehaviour targetBehaviour =
                    weapon.CurrentTarget as NetworkBehaviour;

                if (targetBehaviour != null &&
                    targetBehaviour.NetworkObject != null)
                {
                    StartLaserClientRpc(
                        weapon.SlotIndex,
                        new NetworkObjectReference(
                            targetBehaviour.NetworkObject));
                }

                weapon.NextAttackTime =
                    Time.time +
                    GetFinalAttackInterval(
                        weapon);
            }
        }

        if (weapon.CurrentTarget == null)
            return;

        if (!CanWeaponAttack(weapon))
            return;

        ExecuteLaserDamage(
            weapon,
            weapon.CurrentTarget);

        ApplySelfDamageAfterAttack(weapon);
        ConsumeAmmoAfterAttack(weapon);
        AddStackAfterAttack(weapon);

        if (!weapon.IsReloading &&
            !weapon.IsDisabled)
        {
            weapon.NextAttackTime =
                Time.time +
                GetFinalAttackInterval(
                    weapon);
        }
    }

    private IDamageable FindNearestEnemy(
    float range)
    {
        IDamageable nearest = null;

        float nearestDistanceSquared =
            range * range;

        // =====================================================
        // SHIPS
        // =====================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit target in allShips)
        {
            TrySelectNearestTarget(
                target,
                range,
                ref nearest,
                ref nearestDistanceSquared);
        }

        // =====================================================
        // BASES
        // =====================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit target in allBases)
        {
            TrySelectNearestTarget(
                target,
                range,
                ref nearest,
                ref nearestDistanceSquared);
        }

        return nearest;
    }

    private void TrySelectNearestTarget(
    IDamageable target,
    float range,
    ref IDamageable nearest,
    ref float nearestDistanceSquared)
    {
        if (!IsValidLaserTarget(
                target,
                range))
        {
            return;
        }

        float distanceSquared =
            (
                target.DamageTransform.position -
                ship.transform.position
            ).sqrMagnitude;

        if (distanceSquared >=
            nearestDistanceSquared)
        {
            return;
        }

        nearestDistanceSquared =
            distanceSquared;

        nearest =
            target;
    }

    private bool IsValidLaserTarget(
    IDamageable target,
    float range)
    {
        return IsValidDamageTarget(
            target,
            range);
    }

    private void ExecuteLaserDamage(
    WeaponRuntime weapon,
    IDamageable target)
    {
        if (target == null)
            return;

        if (target.IsDead)
            return;

        float hullDamage =
            GetFinalHullDamage(
                weapon);

        float shieldDamage =
            GetFinalShieldDamage(
                weapon);

        target.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        ApplySlowOnHit(
            weapon,
            target);

        ApplyAoeDamage(
            weapon,
            target,
            target.DamageTransform.position);

        ApplyChainAttack(
            weapon,
            target,
            hullDamage,
            shieldDamage);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[LASER] {ship.name} atakuje " +
                $"{target.DamageTransform.name}. " +
                $"Hull={hullDamage:0.##}, " +
                $"Shield={shieldDamage:0.##}",
                ship);
        }
    }

    private void ClearLaserTarget(
    WeaponRuntime weapon)
    {
        if (weapon == null)
            return;

        weapon.CurrentTarget = null;

        StopLaserClientRpc(
            weapon.SlotIndex);
    }

    [ClientRpc]
    private void StartLaserClientRpc(
        int slotIndex,
        NetworkObjectReference targetReference)
    {
        if (!targetReference.TryGet(
                out NetworkObject targetObject))
        {
            return;
        }

        IDamageable target =
            targetObject.GetComponent<IDamageable>();

        if (target == null)
            return;

        if (laserLines == null ||
            slotIndex < 0 ||
            slotIndex >= laserLines.Length)
        {
            Debug.LogWarning(
                $"[LASER] Brak LineRenderer dla slotu {slotIndex}.",
                this);

            return;
        }

        LineRenderer line =
            laserLines[slotIndex];

        if (line == null)
        {
            Debug.LogWarning(
                $"[LASER] Laser Lines[{slotIndex}] jest pusty.",
                this);

            return;
        }

        StopLaserVisualLocal(slotIndex);

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.gameObject.SetActive(true);

        laserVisuals[slotIndex] =
            new LaserVisual
            {
                Line = line,
                Target = target
            };

        UpdateSingleLaserVisual(
            slotIndex,
            laserVisuals[slotIndex]);
    }

    [ClientRpc]
    private void StopLaserClientRpc(
        int slotIndex)
    {
        StopLaserVisualLocal(
            slotIndex);
    }

    private void UpdateLaserVisuals()
    {
        List<int> lasersToStop = new();

        foreach (
            KeyValuePair<int, LaserVisual> pair
            in laserVisuals)
        {
            int slotIndex = pair.Key;
            LaserVisual visual = pair.Value;

            bool invalid =
                visual == null ||
                visual.Line == null ||
                visual.Target == null ||
                visual.Target.IsDead ||
                visual.Target.DamageTransform == null;

            if (invalid)
            {
                lasersToStop.Add(slotIndex);
                continue;
            }

            UpdateSingleLaserVisual(
                slotIndex,
                visual);
        }

        foreach (int slotIndex in lasersToStop)
        {
            StopLaserVisualLocal(
                slotIndex);
        }
    }

    private void UpdateSingleLaserVisual(
    int slotIndex,
    LaserVisual visual)
    {
        if (visual == null ||
            visual.Line == null ||
            visual.Target == null)
        {
            return;
        }

        Vector3 startPosition =
            transform.position;

        if (laserOrigins != null &&
            slotIndex >= 0 &&
            slotIndex < laserOrigins.Length &&
            laserOrigins[slotIndex] != null)
        {
            startPosition =
                laserOrigins[slotIndex].position;
        }

        visual.Line.SetPosition(
            0,
            startPosition);

        visual.Line.SetPosition(
            1,
            visual.Target.DamageTransform.position);
    }

    private void StopLaserVisualLocal(
        int slotIndex)
    {
        if (laserLines != null &&
            slotIndex >= 0 &&
            slotIndex < laserLines.Length)
        {
            LineRenderer line =
                laserLines[slotIndex];

            if (line != null)
                line.gameObject.SetActive(false);
        }

        laserVisuals.Remove(
            slotIndex);
    }

    public override void OnNetworkDespawn()
    {
        if (laserLines != null)
        {
            foreach (LineRenderer line in laserLines)
            {
                if (line != null)
                    line.gameObject.SetActive(false);
            }
        }

        laserVisuals.Clear();

        base.OnNetworkDespawn();
    }
    private void InitializeLaserVisualsLocal()
    {
        if (laserLines == null)
            return;

        foreach (LineRenderer line in laserLines)
        {
            if (line == null)
                continue;

            line.useWorldSpace = true;
            line.positionCount = 2;
            line.gameObject.SetActive(false);
        }

        laserVisuals.Clear();
    }

    // =========================================================
    // PROJECTILE
    // =========================================================

    private void UpdateProjectileWeapons()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntime weapon = weapons[i];

            if (weapon.Definition == null)
                continue;

            if (weapon.Definition.weaponType != WeaponType.Projectile)
                continue;

            UpdateProjectileWeapon(weapon);
        }
    }

    private void UpdateProjectileWeapon(
        WeaponRuntime weapon)
    {
        float range =
            GetFinalWeaponRange(weapon);

        if (!IsValidLaserTarget(
                weapon.CurrentTarget,
                range))
        {
            weapon.CurrentTarget =
                FindNearestEnemy(range);
        }

        if (weapon.CurrentTarget == null)
            return;

        if (!CanWeaponAttack(weapon))
            return;

        FireProjectile(
            weapon,
            weapon.CurrentTarget);

        if (!weapon.IsReloading && !weapon.IsDisabled)
        {
            weapon.NextAttackTime =
                Time.time +
                GetFinalAttackInterval(
                    weapon);
        }
    }

    private void FireProjectile(
    WeaponRuntime weapon,
    IDamageable target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[PROJECTILE] Brak Projectile Prefab.",
                this);

            return;
        }

        if (target == null)
            return;

        if (target.IsDead)
            return;

        if (target.DamageTransform == null)
            return;

        Vector3 spawnPosition =
            transform.position;

        int slotIndex =
            weapon.SlotIndex;

        if (projectileOrigins != null &&
            slotIndex >= 0 &&
            slotIndex < projectileOrigins.Length &&
            projectileOrigins[slotIndex] != null)
        {
            spawnPosition =
                projectileOrigins[slotIndex].position;
        }

        Vector3 direction =
            target.DamageTransform.position -
            spawnPosition;

        Quaternion spawnRotation =
            direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up)
                : Quaternion.identity;

        NetworkObject projectileObject =
            Instantiate(
                projectilePrefab,
                spawnPosition,
                spawnRotation);

        HomingProjectile projectile =
            projectileObject.GetComponent<HomingProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                "[PROJECTILE] Prefab nie posiada HomingProjectile.",
                projectileObject);

            Destroy(
                projectileObject.gameObject);

            return;
        }

        projectile.Initialize(
            target,

            GetFinalHullDamage(weapon),
            GetFinalShieldDamage(weapon),

            Mathf.Max(
                0.01f,
                weapon.Definition.projectileSpeed),

            ship.ownerId.Value,

            weapon.Definition.canSlowOnHit,
            weapon.Definition.slowPercent,
            weapon.Definition.slowDuration,

            weapon.Definition.weaponHasAoe,
            weapon.Definition.weaponAoeRange,
            weapon.Definition.weaponAoeDamageMultiplier,

            weapon.Definition.canChainAttack,
            weapon.Definition.maxTargets,
            weapon.Definition.chainJumpsRange,
            weapon.Definition.chainDamageMultiplier);

        projectileObject.Spawn();

        ApplySelfDamageAfterAttack(
            weapon);

        ConsumeAmmoAfterAttack(
            weapon);

        AddStackAfterAttack(
            weapon);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PROJECTILE] {ship.name} wystrzeli³ w " +
                $"{target.DamageTransform.name}. " +
                $"Speed={weapon.Definition.projectileSpeed:0.##}",
                ship);
        }
    }

    // =========================================================
    // SELF DAMAG
    // =========================================================

    private void ApplySelfDamageAfterAttack(
    WeaponRuntime weapon)
    {
        if (!IsServer)
            return;

        if (ship == null ||
            ship.isDead.Value ||
            weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.selfHarmOnAttack)
            return;

        if (definition.selfDamage <= 0f)
            return;

        float classSlotMultiplier =
            ship.GetModuleEffectMultiplier(
                weapon.SlotIndex,
                definition);

        float finalSelfDamage =
            definition.selfDamage *
            classSlotMultiplier;

        ship.TakeDirectHullDamage(
            finalSelfDamage);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[SELF DAMAGE] {ship.name} otrzyma³ " +
                $"{finalSelfDamage:0.##} obra¿eñ po ataku. " +
                $"ClassMultiplier=x{classSlotMultiplier:0.##}",
                ship);
        }
    }

    // =========================================================
    // SLOW
    // =========================================================

    private void ApplySlowOnHit(
    WeaponRuntime weapon,
    IDamageable target)
    {
        if (!IsServer)
            return;

        if (weapon == null ||
            weapon.Definition == null ||
            target == null)
        {
            return;
        }

        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.canSlowOnHit)
            return;

        /*
         * Slow dzia³a wy³¹cznie na statki.
         * Bazy s¹ nieruchome.
         */
        if (target is ShipUnit shipTarget)
        {
            shipTarget.ApplySlow(
                definition.slowPercent,
                definition.slowDuration);
        }
    }

    // =========================================================
    // MAGAZINE
    // =========================================================

    private bool CanWeaponAttack(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return false;
        }

        if (weapon.IsDisabled)
            return false;

        if (weapon.IsReloading)
            return false;

        if (Time.time < weapon.NextAttackTime)
            return false;

        // Broñ bez magazynka.
        if (!weapon.Definition.weaponHasMagazine)
            return true;

        // Jest amunicja.
        if (weapon.CurrentAmmo > 0)
            return true;

        // Magazynek pusty.
        HandleEmptyMagazine(weapon);

        return false;
    }

    private void ConsumeAmmoAfterAttack(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        // Broñ bez magazynka niczego nie zu¿ywa.
        if (!weapon.Definition.weaponHasMagazine)
            return;

        if (weapon.IsDisabled)
            return;

        weapon.CurrentAmmo =
            Mathf.Max(
                0,
                weapon.CurrentAmmo - 1);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[MAGAZINE] {ship.name} | " +
                $"slot={weapon.SlotIndex} | " +
                $"ammo={weapon.CurrentAmmo}/" +
                $"{weapon.Definition.magazineCapacity}",
                ship);
        }

        if (weapon.CurrentAmmo <= 0)
        {
            HandleEmptyMagazine(weapon);
        }
    }

    private void HandleEmptyMagazine(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        if (weapon.IsDisabled ||
            weapon.IsReloading)
        {
            return;
        }

        if (weapon.Definition.magazineCanReload)
        {
            StartReload(weapon);
        }
        else
        {
            ConsumeWeaponModule(weapon);
        }
    }

    private void StartReload(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        if (!weapon.Definition.weaponHasMagazine)
            return;

        if (!weapon.Definition.magazineCanReload)
            return;

        if (weapon.IsReloading ||
            weapon.IsDisabled)
        {
            return;
        }

        int capacity =
            Mathf.Max(
                1,
                weapon.Definition.magazineCapacity);

        if (weapon.CurrentAmmo >= capacity)
            return;

        weapon.IsReloading = true;

        weapon.ReloadEndTime =
            Time.time +
            Mathf.Max(
                0f,
                weapon.Definition.magazineReloadTime);

        /*
         * Laser podczas reloadu przestaje œwieciæ.
         * Po zakoñczeniu reloadu ponownie z³apie cel.
         */
        if (weapon.Definition.weaponType ==
            WeaponType.Laser)
        {
            if (weapon.CurrentTarget != null)
                ClearLaserTarget(weapon);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[MAGAZINE] {ship.name} rozpoczyna reload. " +
                $"slot={weapon.SlotIndex}, " +
                $"czas={weapon.Definition.magazineReloadTime:0.##} s",
                ship);
        }
    }

    private void UpdateMagazineReloads()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponRuntime weapon =
                weapons[i];

            if (weapon == null ||
                weapon.Definition == null)
            {
                continue;
            }

            if (!weapon.IsReloading)
                continue;

            if (Time.time <
                weapon.ReloadEndTime)
            {
                continue;
            }

            FinishReload(weapon);
        }
    }

    private void FinishReload(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        weapon.IsReloading = false;

        weapon.CurrentAmmo =
            Mathf.Max(
                1,
                weapon.Definition.magazineCapacity);

        /*
         * Po reloadzie broñ jest gotowa.
         * Nie dok³adamy kolejnego attack interval.
         */
        weapon.NextAttackTime =
            Time.time;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[MAGAZINE] {ship.name} zakoñczy³ reload. " +
                $"slot={weapon.SlotIndex}, " +
                $"ammo={weapon.CurrentAmmo}",
                ship);
        }
    }

    private void ConsumeWeaponModule(
    WeaponRuntime weapon)
    {
        if (!IsServer)
            return;

        if (weapon == null ||
            weapon.IsDisabled)
        {
            return;
        }

        weapon.IsDisabled = true;
        weapon.IsReloading = false;
        weapon.CurrentAmmo = 0;

        if (weapon.Definition != null &&
            weapon.Definition.weaponType ==
            WeaponType.Laser)
        {
            if (weapon.CurrentTarget != null)
                ClearLaserTarget(weapon);

            StopLaserClientRpc(
                weapon.SlotIndex);
        }

        RemoveWeaponModuleFromSlot(
            weapon.SlotIndex);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[MAGAZINE] Modu³ broni ze slotu " +
                $"{weapon.SlotIndex} zosta³ zu¿yty.",
                ship);
        }
    }

    private void RemoveWeaponModuleFromSlot(
        int slotIndex)
    {
        if (!IsServer ||
            ship == null)
        {
            return;
        }

        switch (slotIndex)
        {
            case 0:
                ship.normalModule1.Value = default;
                break;

            case 1:
                ship.normalModule2.Value = default;
                break;

            case 2:
                ship.normalModule3.Value = default;
                break;

            case 3:
                ship.classModule.Value = default;
                break;
        }
    }

    // =========================================================
    // STACKING
    // =========================================================

    private void AddStackAfterAttack(
        WeaponRuntime weapon)
    {
        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.weaponIsStacking)
            return;

        int maxStacks =
            Mathf.Max(
                0,
                definition.weaponMaxStacks);

        if (maxStacks <= 0)
            return;

        weapon.CurrentStacks =
            Mathf.Min(
                weapon.CurrentStacks + 1,
                maxStacks);

        weapon.LastAttackTime =
            Time.time;

        RecalculateStackMovementSpeed();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[STACK] {ship.name} | " +
                $"slot={weapon.SlotIndex} | " +
                $"stacks={weapon.CurrentStacks}/{maxStacks}",
                ship);
        }
    }

    // =========================================================
    // AOE
    // =========================================================

    private void ApplyAoeDamage(
    WeaponRuntime weapon,
    IDamageable primaryTarget,
    Vector3 hitPosition)
    {
        if (!IsServer)
            return;

        if (weapon == null ||
            weapon.Definition == null)
        {
            return;
        }

        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.weaponHasAoe)
            return;

        float aoeRange =
            Mathf.Max(
                0f,
                definition.weaponAoeRange);

        if (aoeRange <= 0f)
            return;

        float damageMultiplier =
            Mathf.Max(
                0f,
                definition.weaponAoeDamageMultiplier);

        float aoeHullDamage =
            GetFinalHullDamage(weapon) *
            damageMultiplier;

        float aoeShieldDamage =
            GetFinalShieldDamage(weapon) *
            damageMultiplier;

        float rangeSquared =
            aoeRange * aoeRange;

        // =========================================================
        // SHIPS
        // =========================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit target in allShips)
        {
            ApplyAoeToTarget(
                weapon,
                primaryTarget,
                target,
                hitPosition,
                rangeSquared,
                aoeHullDamage,
                aoeShieldDamage);
        }

        // =========================================================
        // BASES
        // =========================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit target in allBases)
        {
            ApplyAoeToTarget(
                weapon,
                primaryTarget,
                target,
                hitPosition,
                rangeSquared,
                aoeHullDamage,
                aoeShieldDamage);
        }
    }

    [ClientRpc]
    private void ShowAoeImpactClientRpc(
    Vector3 position,
    float radius)
    {
        if (aoeImpactPrefab == null)
            return;

        GameObject visual =
            Instantiate(
                aoeImpactPrefab,
                position,
                Quaternion.identity);

        AoeImpactVisual impact =
            visual.GetComponent<AoeImpactVisual>();

        if (impact != null)
        {
            impact.Initialize(radius);
        }
    }

    // =========================================================
    // ChainAttack
    // =========================================================

    private void ApplyChainAttack(
    WeaponRuntime weapon,
    IDamageable primaryTarget,
    float initialHullDamage,
    float initialShieldDamage)
    {
        if (!IsServer)
            return;

        if (weapon == null ||
            weapon.Definition == null ||
            primaryTarget == null)
        {
            return;
        }

        ModuleDefinition definition =
            weapon.Definition;

        if (!definition.canChainAttack)
            return;

        int maxJumps =
            Mathf.Max(
                0,
                definition.maxTargets);

        if (maxJumps <= 0)
            return;

        float jumpRange =
            Mathf.Max(
                0f,
                definition.chainJumpsRange);

        if (jumpRange <= 0f)
            return;

        float multiplier =
            Mathf.Max(
                0f,
                definition.chainDamageMultiplier);

        List<IDamageable> alreadyHit =
     new List<IDamageable>();

        alreadyHit.Add(primaryTarget);

        IDamageable currentTarget =
            primaryTarget;

        float currentHullDamage =
            initialHullDamage;

        float currentShieldDamage =
            initialShieldDamage;

        for (int jump = 0;
             jump < maxJumps;
             jump++)
        {
            IDamageable nextTarget =
                FindNearestChainTarget(
                    currentTarget,
                    jumpRange,
                    alreadyHit);

            if (nextTarget == null)
                break;

            currentHullDamage *=
                multiplier;

            currentShieldDamage *=
                multiplier;

            // Wizualizacja przeskoku chaina.
            ShowChainVisualClientRpc(
                currentTarget.DamageTransform.position,
                nextTarget.DamageTransform.position);

            // Damage kolejnego celu.
            nextTarget.TakeWeaponDamage(
                currentHullDamage,
                currentShieldDamage);

            ApplySlowOnHit(
                weapon,
                nextTarget);

            alreadyHit.Add(
                nextTarget);

            currentTarget =
                nextTarget;
        }
    }

    private IDamageable FindNearestChainTarget(
    IDamageable fromTarget,
    float range,
    List<IDamageable> alreadyHit)
    {
        if (fromTarget == null)
            return null;

        if (fromTarget.DamageTransform == null)
            return null;

        IDamageable nearest = null;

        float nearestDistanceSquared =
            range * range;

        // =========================================================
        // SHIPS
        // =========================================================

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit target in allShips)
        {
            TrySelectChainTarget(
                fromTarget,
                target,
                alreadyHit,
                ref nearest,
                ref nearestDistanceSquared);
        }

        // =========================================================
        // BASES
        // =========================================================

        BaseUnit[] allBases =
            FindObjectsByType<BaseUnit>(
                FindObjectsSortMode.None);

        foreach (BaseUnit target in allBases)
        {
            TrySelectChainTarget(
                fromTarget,
                target,
                alreadyHit,
                ref nearest,
                ref nearestDistanceSquared);
        }

        return nearest;
    }

    private void TrySelectChainTarget(
    IDamageable fromTarget,
    IDamageable target,
    List<IDamageable> alreadyHit,
    ref IDamageable nearest,
    ref float nearestDistanceSquared)
    {
        if (target == null)
            return;

        if (target.IsDead)
            return;

        if (target.DamageTransform == null)
            return;

        // Friendly fire wy³¹czony.
        if (target.OwnerId ==
            ship.ownerId.Value)
        {
            return;
        }

        if (alreadyHit.Contains(target))
            return;

        Vector3 offset =
            target.DamageTransform.position -
            fromTarget.DamageTransform.position;

        float distanceSquared =
            offset.sqrMagnitude;

        if (distanceSquared >
            nearestDistanceSquared)
        {
            return;
        }

        nearestDistanceSquared =
            distanceSquared;

        nearest =
            target;
    }


    [ClientRpc]
    private void ShowChainVisualClientRpc(
    Vector3 startPosition,
    Vector3 endPosition)
    {
        if (chainLinePrefab == null)
            return;

        LineRenderer line =
            Instantiate(chainLinePrefab);

        line.positionCount = 2;
        line.useWorldSpace = true;

        line.SetPosition(0, startPosition);
        line.SetPosition(1, endPosition);

        Destroy(
            line.gameObject,
            chainVisualLifetime);
    }

    private bool IsValidDamageTarget(
    IDamageable target,
    float range)
    {
        if (target == null)
            return false;

        /*
         * IDamageable jest interfejsem.
         * Sprawdzamy wiêc równie¿ w³aœciwy
         * NetworkBehaviour Unity.
         */
        NetworkBehaviour targetBehaviour =
            target as NetworkBehaviour;

        if (targetBehaviour == null)
            return false;

        if (!targetBehaviour.IsSpawned)
            return false;

        if (target.IsDead)
            return false;

        if (target.DamageTransform == null)
            return false;

        if (target.OwnerId ==
            ship.ownerId.Value)
        {
            return false;
        }

        float distanceSquared =
            (
                target.DamageTransform.position -
                ship.transform.position
            ).sqrMagnitude;

        return distanceSquared <=
               range * range;
    }

    private void ApplyAoeToTarget(
    WeaponRuntime weapon,
    IDamageable primaryTarget,
    IDamageable target,
    Vector3 hitPosition,
    float rangeSquared,
    float hullDamage,
    float shieldDamage)
    {
        if (target == null)
            return;

        if (ReferenceEquals(
                target,
                primaryTarget))
        {
            return;
        }

        if (target.IsDead)
            return;

        if (target.OwnerId ==
            ship.ownerId.Value)
        {
            return;
        }

        if (target.DamageTransform == null)
            return;

        Vector3 offset =
            target.DamageTransform.position -
            hitPosition;

        if (offset.sqrMagnitude >
            rangeSquared)
        {
            return;
        }

        target.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        ApplySlowOnHit(
            weapon,
            target);
    }
}