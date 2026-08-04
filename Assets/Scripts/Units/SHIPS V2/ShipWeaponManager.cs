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

    [Header("Laser Visuals")]
    [SerializeField] private Transform[] laserOrigins = new Transform[4];
    [SerializeField] private LineRenderer[] laserLines = new LineRenderer[4];

    private sealed class LaserVisual
    {
        public LineRenderer Line;
        public ShipUnit Target;
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

            if (Time.time < weapon.NextAttackTime)
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

            weapon.NextAttackTime =
                Time.time +
                GetFinalAttackInterval(
                    weapon.Definition);
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
            Mathf.Max(
                0f,
                weapon.Definition.weaponRange);

        float hullDamage =
            GetFinalHullDamage(
                weapon.Definition);

        float shieldDamage =
            GetFinalShieldDamage(
                weapon.Definition);

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        int targetsHit = 0;

        foreach (ShipUnit target in allShips)
        {
            if (!IsValidAuraTarget(
                    target,
                    range))
            {
                continue;
            }

            target.TakeWeaponDamage(
                hullDamage,
                shieldDamage);

            targetsHit++;
        }

        if (showDebugLogs && targetsHit > 0)
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
    private bool IsValidAuraTarget(
    ShipUnit target,
    float range)
    {
        if (target == null)
            return false;

        if (target == ship)
            return false;

        if (!target.IsSpawned)
            return false;

        if (target.isDead.Value)
            return false;

        if (target.ownerId.Value ==
            ship.ownerId.Value)
        {
            return false;
        }

        float distance =
            Vector3.Distance(
                ship.transform.position,
                target.transform.position);

        return distance <= range;
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
        float range = Mathf.Max(
            0f,
            weapon.Definition.weaponRange);

        if (!IsValidLaserTarget(
                weapon.CurrentTarget,
                range))
        {
            if (weapon.CurrentTarget != null)
                ClearLaserTarget(weapon);

            weapon.CurrentTarget =
                FindNearestEnemy(range);

            if (weapon.CurrentTarget != null)
            {
                StartLaserClientRpc(
                    weapon.SlotIndex,
                    new NetworkObjectReference(
                        weapon.CurrentTarget.NetworkObject));

                // Pierwsze obra¿enia dopiero po pe³nym interwale.
                weapon.NextAttackTime =
                    Time.time +
                    GetFinalAttackInterval(
                        weapon.Definition);
            }
        }

        if (weapon.CurrentTarget == null)
            return;

        if (Time.time < weapon.NextAttackTime)
            return;

        ExecuteLaserDamage(
            weapon,
            weapon.CurrentTarget);

        ApplySelfDamageAfterAttack(weapon);

        weapon.NextAttackTime =
            Time.time +
            GetFinalAttackInterval(
                weapon.Definition);
    }

    private ShipUnit FindNearestEnemy(
        float range)
    {
        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        ShipUnit nearest = null;

        float nearestDistanceSquared =
            range * range;

        foreach (ShipUnit target in allShips)
        {
            if (!IsValidLaserTarget(
                    target,
                    range))
            {
                continue;
            }

            float distanceSquared =
                (target.transform.position -
                 ship.transform.position)
                .sqrMagnitude;

            if (distanceSquared >=
                nearestDistanceSquared)
            {
                continue;
            }

            nearestDistanceSquared =
                distanceSquared;

            nearest = target;
        }

        return nearest;
    }

    private bool IsValidLaserTarget(
        ShipUnit target,
        float range)
    {
        if (target == null)
            return false;

        if (target == ship)
            return false;

        if (!target.IsSpawned)
            return false;

        if (target.isDead.Value)
            return false;

        if (target.ownerId.Value ==
            ship.ownerId.Value)
        {
            return false;
        }

        float distanceSquared =
            (target.transform.position -
             ship.transform.position)
            .sqrMagnitude;

        return distanceSquared <=
               range * range;
    }

    private void ExecuteLaserDamage(
        WeaponRuntime weapon,
        ShipUnit target)
    {
        if (target == null)
            return;

        float hullDamage =
            GetFinalHullDamage(
                weapon.Definition);

        float shieldDamage =
            GetFinalShieldDamage(
                weapon.Definition);

        target.TakeWeaponDamage(
            hullDamage,
            shieldDamage);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[LASER] {ship.name} atakuje {target.name}. " +
                $"Hull={hullDamage:0.##}, " +
                $"Shield={shieldDamage:0.##}",
                ship);
        }
    }

    private void ClearLaserTarget(
        WeaponRuntime weapon)
    {
        if (weapon.CurrentTarget == null)
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

        ShipUnit target =
            targetObject.GetComponent<ShipUnit>();

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
                !visual.Target.IsSpawned ||
                visual.Target.isDead.Value;

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
            visual.Target.transform.position);
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
        float range = Mathf.Max(
            0f,
            weapon.Definition.weaponRange);

        if (!IsValidLaserTarget(
                weapon.CurrentTarget,
                range))
        {
            weapon.CurrentTarget =
                FindNearestEnemy(range);
        }

        if (weapon.CurrentTarget == null)
            return;

        if (Time.time < weapon.NextAttackTime)
            return;

        FireProjectile(
            weapon,
            weapon.CurrentTarget);

        weapon.NextAttackTime =
            Time.time +
            GetFinalAttackInterval(
                weapon.Definition);
    }

    private void FireProjectile(
        WeaponRuntime weapon,
        ShipUnit target)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[PROJECTILE] Brak Projectile Prefab.",
                this);

            return;
        }

        if (target == null ||
            !target.IsSpawned ||
            target.isDead.Value)
        {
            return;
        }

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
            target.transform.position -
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

            Destroy(projectileObject.gameObject);
            return;
        }

        projectile.Initialize(
            target,
            GetFinalHullDamage(weapon.Definition),
            GetFinalShieldDamage(weapon.Definition),
            Mathf.Max(
                0.01f,
                weapon.Definition.projectileSpeed));

        projectileObject.Spawn();

        ApplySelfDamageAfterAttack(weapon);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PROJECTILE] {ship.name} wystrzeli³ w {target.name}. " +
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

        ship.TakeDirectHullDamage(
            definition.selfDamage);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[SELF DAMAGE] {ship.name} otrzyma³ " +
                $"{definition.selfDamage:0.##} obra¿eñ po ataku.",
                ship);
        }
    }
}