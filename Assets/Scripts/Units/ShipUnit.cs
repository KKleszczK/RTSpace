using Unity.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ShipUnit : NetworkBehaviour, IDamageable
{
    // =========================================================
    // IDENTITY
    // =========================================================

    [Header("Identity")]
    public NetworkVariable<FixedString64Bytes> instanceId = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> shipId = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> ownerId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // =========================================================
    // STATS
    // =========================================================

    [Header("Rotation")]
    [SerializeField] private Transform shipModel;
    [SerializeField] private float rotationSpeed = 180f;

    [Header("Base Stats")]
    [SerializeField] private int baseMaxHp = 0;
    [SerializeField] private int baseMaxShield = 0;
    [SerializeField] private float baseMoveSpeed = 5f;

    [Header("Final Deployed Stats")]

    public NetworkVariable<int> maxHp = new(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

    public NetworkVariable<int> maxShield = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<float> moveSpeed = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Map Bounds")]
    [SerializeField] private float mapSize = 100f;
    [SerializeField] private float mapBorderMargin = 0f;

    private float weaponsDamageMultiplier = 1f;
    private float weaponsAttackSpeedMultiplier = 1f;

    public NetworkVariable<int> hp = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> shield = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isDead = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<ShipSocketState> SocketState = new(
        ShipSocketState.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public int MaxHp => maxHp.Value;
    public int MaxShield => maxShield.Value;
    public float MoveSpeed => moveSpeed.Value;

    // =========================================================
    // IDAMAGEABLE
    // =========================================================

    public ulong OwnerId =>
        ownerId.Value;

    public bool IsDead =>
        isDead.Value;

    public Transform DamageTransform =>
        transform;

    public float WeaponsDamageMultiplier =>
        weaponsDamageMultiplier;

    public float WeaponsAttackSpeedMultiplier =>
        weaponsAttackSpeedMultiplier; 

    // =========================================================
    // MODULES
    // =========================================================

    [Header("Modules")]
    public NetworkVariable<FixedString64Bytes> normalModule1 = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> normalModule2 = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> normalModule3 = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> classModule = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    private NetworkVariable<Vector3> targetPosition = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);


    // =========================================================
    // UI
    // =========================================================

    [Header("Selection")]
    [SerializeField] private GameObject selectionMarker;

    [Header("HP UI")]
    [SerializeField] private RectTransform currentHpBar;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private float maxHpBarWidth = 1080f;

    [Header("Shield UI")]
    [SerializeField] private RectTransform currentShieldBar;
    [SerializeField] private TMP_Text shieldText;
    [SerializeField] private float maxShieldBarWidth = 1080f;

    private Renderer rend;


    [Header("Combat Feedback")]
    [SerializeField] private Transform combatTextOrigin;
    [SerializeField] private CombatFloatingText combatTextPrefab;

    // =========================================================
    // NETWORK
    // =========================================================

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rend = GetComponentInChildren<Renderer>();

        ownerId.OnValueChanged += OnOwnerChanged;
        hp.OnValueChanged += OnHpChanged;
        shield.OnValueChanged += OnShieldChanged;
        maxHp.OnValueChanged += OnMaxHpChanged;
        maxShield.OnValueChanged += OnMaxShieldChanged;

        if (IsServer)
        {
            targetPosition.Value =
                transform.position;
        }

        SetSelectedLocal(false);
        ApplyColor();
        UpdateHpBar();
        UpdateShieldBar();
    }

    public override void OnNetworkDespawn()
    {
        ownerId.OnValueChanged -= OnOwnerChanged;
        hp.OnValueChanged -= OnHpChanged;
        shield.OnValueChanged -= OnShieldChanged;

        maxHp.OnValueChanged -= OnMaxHpChanged;
        maxShield.OnValueChanged -= OnMaxShieldChanged;

        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        UpdateSlows();
        UpdateMovement();
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    public void InitializeFromDockedShip(
        DockedShipData data,
        ShipDefinition definition,
        ulong newOwnerId)
    {
        if (!IsServer)
            return;

        instanceId.Value =
            data.instanceId;

        shipId.Value =
            data.shipId;

        ownerId.Value =
            newOwnerId;

        normalModule1.Value =
            data.normalModule1;

        normalModule2.Value =
            data.normalModule2;

        normalModule3.Value =
            data.normalModule3;

        classModule.Value =
            data.classModule;

        weaponStackMovementPercent = 0f;

        if (definition != null)
        {
            baseMaxHp = definition.maxHp;
            baseMaxShield = definition.maxShield;
            baseMoveSpeed = definition.moveSpeed;
        }

        
        CalculateDeployedModuleStats();

        ShipWeaponManager weaponManager =
        GetComponent<ShipWeaponManager>();

        if (weaponManager != null)
        {
            weaponManager.InitializeWeaponsFromShipModules();
        }

        hp.Value =
            maxHp.Value;

        shield.Value =
            maxShield.Value;

        isDead.Value =
            false;

        targetPosition.Value =
            transform.position;

        Debug.Log(
            $"[SHIP INIT] " +
            $"instanceId={instanceId.Value}, " +
            $"shipId={shipId.Value}, " +
            $"owner={ownerId.Value}");
    }

    public DockedShipData CreateDockedShipData()
    {
        return new DockedShipData
        {
            instanceId =
                instanceId.Value,

            shipId =
                shipId.Value,

            normalModule1 =
                normalModule1.Value,

            normalModule2 =
                normalModule2.Value,

            normalModule3 =
                normalModule3.Value,

            classModule =
                classModule.Value
        };
    }

    // =========================================================
    // OWNERSHIP
    // =========================================================

    public bool IsMine()
    {
        if (NetworkManager.Singleton == null)
            return false;

        return ownerId.Value ==
               NetworkManager.Singleton.LocalClientId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetOwnerServerRpc(
        ulong newOwnerId,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (senderClientId != newOwnerId)
            return;

        ownerId.Value =
            newOwnerId;
    }

    private void OnOwnerChanged(
        ulong oldValue,
        ulong newValue)
    {
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (rend == null)
        {
            rend =
                GetComponentInChildren<Renderer>();
        }

        if (rend == null)
            return;

        if (ownerId.Value == 0)
            rend.material.color = Color.blue;
        else
            rend.material.color = Color.red;
    }

    // =========================================================
    // SELECTION
    // =========================================================

    public void SetSelectedLocal(bool selected)
    {
        if (selectionMarker != null)
        {
            selectionMarker.SetActive(
                selected);
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    [ServerRpc(RequireOwnership = false)]
    public void MoveToServerRpc(
        Vector3 position,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (senderClientId != ownerId.Value)
            return;

        if (isDead.Value)
            return;

        targetPosition.Value =
            position;
    }

    private void UpdateMovement()
    {
        if (isDead.Value)
            return;

        Vector3 direction =
            targetPosition.Value -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        RotateTowardsMovement(direction);

        Vector3 newPosition =
            Vector3.MoveTowards(
                transform.position,
                targetPosition.Value,
                CurrentMoveSpeed * Time.deltaTime);

        float halfMapSize =
            mapSize * 0.5f;

        float minX =
            -halfMapSize + mapBorderMargin;

        float maxX =
            halfMapSize - mapBorderMargin;

        float minZ =
            -halfMapSize + mapBorderMargin;

        float maxZ =
            halfMapSize - mapBorderMargin;

        newPosition.x =
            Mathf.Clamp(
                newPosition.x,
                minX,
                maxX);

        newPosition.z =
            Mathf.Clamp(
                newPosition.z,
                minZ,
                maxZ);

        transform.position =
            newPosition;
    }

    // =========================================================
    // MODULES
    // =========================================================

    public FixedString64Bytes GetModule(
        int slotIndex)
    {
        return slotIndex switch
        {
            0 => normalModule1.Value,
            1 => normalModule2.Value,
            2 => normalModule3.Value,
            3 => classModule.Value,
            _ => default
        };
    }

    public bool HasModule(
        int slotIndex)
    {
        return !GetModule(slotIndex).IsEmpty;
    }

    public bool HasFreeNormalSlot()
    {
        return normalModule1.Value.IsEmpty ||
               normalModule2.Value.IsEmpty ||
               normalModule3.Value.IsEmpty;
    }

    public bool IsSlotFree(
        int slotIndex)
    {
        return GetModule(slotIndex).IsEmpty;
    }

    public int GetFreeNormalSlotCount()
    {
        int count = 0;

        if (normalModule1.Value.IsEmpty)
            count++;

        if (normalModule2.Value.IsEmpty)
            count++;

        if (normalModule3.Value.IsEmpty)
            count++;

        return count;
    }

    public void SetModuleServer(
        int slotIndex,
        FixedString64Bytes moduleId)
    {
        if (!IsServer)
            return;

        switch (slotIndex)
        {
            case 0:
                normalModule1.Value =
                    moduleId;
                break;

            case 1:
                normalModule2.Value =
                    moduleId;
                break;

            case 2:
                normalModule3.Value =
                    moduleId;
                break;

            case 3:
                classModule.Value =
                    moduleId;
                break;
        }
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeWeaponDamage(
        float hullDamage,
        float shieldDamage)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        // =====================================================
        // SHIELD DAMAGE
        // =====================================================

        if (shield.Value > 0)
        {
            int requestedShieldDamage =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(shieldDamage));

            /*
             * Liczymy faktyczny damage.
             *
             * Jeœli tarcza ma 20,
             * a broñ zadaje 100,
             * poka¿emy -20, a nie -100.
             */
            int actualShieldDamage =
                Mathf.Min(
                    shield.Value,
                    requestedShieldDamage);

            if (actualShieldDamage <= 0)
                return;

            shield.Value -=
                actualShieldDamage;

            ShowShieldDamageClientRpc(
                actualShieldDamage);

            // Nadwy¿ka obra¿eñ NIE przechodzi na HP.
            return;
        }

        // =====================================================
        // HULL DAMAGE
        // =====================================================

        int requestedHullDamage =
            Mathf.Max(
                0,
                Mathf.RoundToInt(hullDamage));

        int actualHullDamage =
            Mathf.Min(
                hp.Value,
                requestedHullDamage);

        if (actualHullDamage <= 0)
            return;

        hp.Value -=
            actualHullDamage;

        ShowHullDamageClientRpc(
            actualHullDamage);

        if (hp.Value <= 0)
        {
            hp.Value = 0;
            Die();
        }
    }

    /// <summary>
    /// Obra¿enia omijaj¹ce tarczê.
    /// U¿ywane np. przez Self Damage.
    /// </summary>
    public void TakeDirectHullDamage(
        float damage)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        int requestedDamage =
            Mathf.Max(
                0,
                Mathf.RoundToInt(damage));

        int actualDamage =
            Mathf.Min(
                hp.Value,
                requestedDamage);

        if (actualDamage <= 0)
            return;

        hp.Value -=
            actualDamage;

        ShowHullDamageClientRpc(
            actualDamage);

        if (hp.Value <= 0)
        {
            hp.Value = 0;
            Die();
        }
    }

    // =========================================================
    // COMBAT FEEDBACK
    // =========================================================

    [ClientRpc]
    private void ShowHullDamageClientRpc(
        int damage)
    {
        ShowCombatTextLocal(
            $"-{damage}",
            new Color(
                1f,
                0.2f,
                0.2f,
                1f));
    }

    [ClientRpc]
    private void ShowShieldDamageClientRpc(
        int damage)
    {
        ShowCombatTextLocal(
            $"-{damage}",
            new Color(
                0.2f,
                0.7f,
                1f,
                1f));
    }

    private void ShowCombatTextLocal(
        string value,
        Color color)
    {
        if (combatTextPrefab == null)
            return;

        Vector3 spawnPosition =
            combatTextOrigin != null
                ? combatTextOrigin.position
                : transform.position;

        CombatFloatingText floatingText =
            Instantiate(
                combatTextPrefab,
                spawnPosition,
                Quaternion.identity);

        floatingText.Initialize(
            value,
            color);
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void Die()
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        isDead.Value = true;

        PlayerUnits units =
            FindPlayerUnits(
                ownerId.Value);

        if (units != null)
        {
            units.ReleaseUnit();
        }

        NetworkObject.Despawn(true);
    }

    private PlayerUnits FindPlayerUnits(
        ulong clientId)
    {
        PlayerUnits[] allUnits =
            FindObjectsByType<PlayerUnits>(
                FindObjectsSortMode.None);

        foreach (PlayerUnits playerUnits in allUnits)
        {
            if (!playerUnits.IsSpawned)
                continue;

            if (playerUnits.OwnerClientId ==
                clientId)
            {
                return playerUnits;
            }
        }

        return null;
    }


    // =========================================================
    // SLOW
    // =========================================================

    private sealed class ActiveSlow
    {
        public float Percent;
        public float EndTime;
    }

    private readonly List<ActiveSlow> activeSlows = new();

    public NetworkVariable<float> currentSlowPercent = new(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentSlowPercent =>
        currentSlowPercent.Value;

    public float CurrentMoveSpeed
    {
        get
        {
            float stackMultiplier =
                Mathf.Max(
                    0f,
                    1f + weaponStackMovementPercent / 100f);

            float slowMultiplier =
                Mathf.Clamp01(
                    1f - currentSlowPercent.Value / 100f);

            return moveSpeed.Value *
                stackMultiplier *
                slowMultiplier;
        }
    }

    public void ApplySlow(
    float slowPercent,
    float duration)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        slowPercent =
            Mathf.Clamp(
                slowPercent,
                0f,
                100f);

        duration =
            Mathf.Max(
                0f,
                duration);

        if (slowPercent <= 0f ||
            duration <= 0f)
        {
            return;
        }

        activeSlows.Add(
            new ActiveSlow
            {
                Percent = slowPercent,
                EndTime = Time.time + duration
            });

        RecalculateStrongestSlow();
    }


    private void UpdateSlows()
    {
        bool removedAny = false;

        for (int i = activeSlows.Count - 1;
             i >= 0;
             i--)
        {
            if (Time.time <
                activeSlows[i].EndTime)
            {
                continue;
            }

            activeSlows.RemoveAt(i);
            removedAny = true;
        }

        if (removedAny)
            RecalculateStrongestSlow();
    }

    private void RecalculateStrongestSlow()
    {
        float strongestSlow = 0f;

        foreach (ActiveSlow slow in activeSlows)
        {
            if (slow.Percent > strongestSlow)
                strongestSlow = slow.Percent;
        }

        currentSlowPercent.Value =
            strongestSlow;
    }




    // =========================================================
    // UI
    // =========================================================

    private void OnHpChanged(
        int oldValue,
        int newValue)
    {
        UpdateHpBar();
    }

    private void OnShieldChanged(
        int oldValue,
        int newValue)
    {
        UpdateShieldBar();
    }

    private void OnMaxHpChanged(
    int oldValue,
    int newValue)
    {
        UpdateHpBar();
    }

    private void OnMaxShieldChanged(
        int oldValue,
        int newValue)
    {
        UpdateShieldBar();
    }

    private void UpdateHpBar()
    {
        if (currentHpBar != null)
        {
            float percent =
                maxHp.Value > 0
                    ? (float)hp.Value / maxHp.Value
                    : 0f;

            percent =
                Mathf.Clamp01(percent);

            Vector2 size =
                currentHpBar.sizeDelta;

            size.x =
                maxHpBarWidth *
                percent;

            currentHpBar.sizeDelta =
                size;
        }

        if (hpText != null)
        {
            hpText.text =
                $"{hp.Value}/{maxHp.Value}";
        }
    }

    private void UpdateShieldBar()
    {
        if (currentShieldBar != null)
        {
            float percent =
                maxShield.Value > 0
                    ? (float)shield.Value / maxShield.Value
                    : 0f;

            percent =
                Mathf.Clamp01(percent);

            Vector2 size =
                currentShieldBar.sizeDelta;

            size.x =
                maxShieldBarWidth *
                percent;

            currentShieldBar.sizeDelta =
                size;
        }

        if (shieldText != null)
        {
            shieldText.text =
                $"{shield.Value}/{maxShield.Value}";
        }
    }

    private void RotateTowardsMovement(Vector3 movementDirection)
    {
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(movementDirection.normalized, Vector3.up);

        shipModel.rotation = Quaternion.RotateTowards(
            shipModel.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    private void CalculateDeployedModuleStats()
    {
        if (!IsServer)
            return;

        ModuleStatTotals totals =
            new ModuleStatTotals();

        AddModuleStats(
            normalModule1.Value,
            0,
            ref totals);

        AddModuleStats(
            normalModule2.Value,
            1,
            ref totals);

        AddModuleStats(
            normalModule3.Value,
            2,
            ref totals);

        AddModuleStats(
            classModule.Value,
            3,
            ref totals);

        PlayerUpgradeStats upgrades =
    FindPlayerUpgradeStats(
        ownerId.Value);

        float researchHpFlat = 0f;
        float researchHpPercent = 0f;

        float researchShieldFlat = 0f;
        float researchShieldPercent = 0f;

        float researchSpeedFlat = 0f;
        float researchSpeedPercent = 0f;

        if (upgrades != null)
        {
            researchHpFlat =
                upgrades.shipHpFlat.Value;

            researchHpPercent =
                upgrades.shipHpPercent.Value;

            researchShieldFlat =
                upgrades.shipShieldFlat.Value;

            researchShieldPercent =
                upgrades.shipShieldPercent.Value;

            researchSpeedFlat =
                upgrades.shipSpeedFlat.Value;

            researchSpeedPercent =
                upgrades.shipSpeedPercent.Value;
        }

        float calculatedMaxHp =
    (
        baseMaxHp +
        totals.HpFlat +
        researchHpFlat
    )
    *
    (
        1f +
        (
            totals.HpPercent +
            researchHpPercent
        ) / 100f
    );

        float calculatedMaxShield =
            (
                baseMaxShield +
                totals.ShieldFlat +
                researchShieldFlat
            )
            *
            (
                1f +
                (
                    totals.ShieldPercent +
                    researchShieldPercent
                ) / 100f
            );

        float calculatedMoveSpeed =
            (
                baseMoveSpeed +
                totals.MoveSpeedFlat +
                researchSpeedFlat
            )
            *
            (
                1f +
                (
                    totals.MoveSpeedPercent +
                    researchSpeedPercent
                ) / 100f
            );

        maxHp.Value =
    Mathf.Max(
        1,
        Mathf.RoundToInt(calculatedMaxHp));

        maxShield.Value =
            Mathf.Max(
                0,
                Mathf.RoundToInt(calculatedMaxShield));

        moveSpeed.Value =
            Mathf.Max(
                0f,
                calculatedMoveSpeed);

        weaponsDamageMultiplier =
            Mathf.Max(
                0f,
                1f +
                totals.WeaponsDamagePercent / 100f);

        weaponsAttackSpeedMultiplier =
            Mathf.Max(
                0.01f,
                1f +
                totals.WeaponsAttackSpeedPercent / 100f);

        Debug.Log(
            $"[SHIP STATS] " +
            $"HP={maxHp.Value}, " +
            $"Shield={maxShield.Value}, " +
            $"Speed={moveSpeed.Value:0.##}, " +
            $"Damage x{weaponsDamageMultiplier:0.##}, " +
            $"AttackSpeed x{weaponsAttackSpeedMultiplier:0.##}",
            this);
    }

    private void AddModuleStats(
    FixedString64Bytes moduleId,
    int slotIndex,
    ref ModuleStatTotals totals)
    {
        if (moduleId.IsEmpty)
            return;

        if (!TryGetModuleDefinition(
                moduleId,
                out ModuleDefinition module))
        {
            Debug.LogWarning(
                $"[SHIP STATS] Nie znaleziono modu³u: {moduleId}",
                this);

            return;
        }

        float multiplier =
            GetModuleSlotMultiplier(
                module,
                slotIndex);

        totals.Add(
            module,
            multiplier);

        Debug.Log(
            $"[MODULE MULTIPLIER] " +
            $"module={module.moduleId}, " +
            $"slot={slotIndex}, " +
            $"exclusive={module.exclusive}, " +
            $"type={module.type}, " +
            $"multiplier=x{multiplier}",
            this);
    }

    private float GetModuleSlotMultiplier(
    ModuleDefinition module,
    int slotIndex)
    {
        if (module == null)
            return 1f;

        // Sloty 0-2 zawsze dzia³aj¹ normalnie.
        if (slotIndex != 3)
            return 1f;

        // Exclusive modu³ w ClassSlot zawsze dzia³a x1.
        if (module.exclusive)
            return 1f;

        // Zwyk³y modu³ w ClassSlot:
        // zgodny typ statku -> x2
        // inny typ -> x1
        if (IsModuleTypeCompatibleWithCurrentShip(module))
            return 2f;

        return 1f;
    }

    private bool IsModuleTypeCompatibleWithCurrentShip(
        ModuleDefinition module)
    {
        if (module == null)
            return false;

        if (ShipDatabase.Instance == null)
            return false;

        if (shipId.Value.IsEmpty)
            return false;

        ShipDefinition shipDefinition =
            ShipDatabase.Instance.GetShip(
                shipId.Value.ToString());

        if (shipDefinition == null)
            return false;

        return string.Equals(
            module.type.ToString(),
            shipDefinition.shipType.ToString(),
            System.StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetModuleDefinition(
    FixedString64Bytes moduleId,
    out ModuleDefinition module)
    {
        module = null;

        if (ModuleDatabase.Instance == null)
            return false;

        module = ModuleDatabase.Instance.GetModule(moduleId.ToString());

        return module != null;
    }

    private struct ModuleStatTotals
    {
        public float ShieldFlat;
        public float ShieldPercent;

        public float HpFlat;
        public float HpPercent;

        public float MoveSpeedFlat;
        public float MoveSpeedPercent;

        public float WeaponsDamagePercent;
        public float WeaponsAttackSpeedPercent;

        public void Add(
    ModuleDefinition module,
    float multiplier)
        {
            if (module == null)
                return;

            ShieldFlat +=
                module.shieldFlat *
                multiplier;

            ShieldPercent +=
                module.shieldPercent *
                multiplier;

            HpFlat +=
                module.hpFlat *
                multiplier;

            HpPercent +=
                module.hpPercent *
                multiplier;

            MoveSpeedFlat +=
                module.moveSpeedFlat *
                multiplier;

            MoveSpeedPercent +=
                module.moveSpeedPercent *
                multiplier;

            WeaponsDamagePercent +=
                module.allWeaponsDamagePercent *
                multiplier;

            WeaponsAttackSpeedPercent +=
                module.allWeaponsAttackSpeedPercent *
                multiplier;
        }
    }

    private float weaponStackMovementPercent;

    public void SetWeaponStackMovementPercent(
        float percent)
    {
        if (!IsServer)
            return;

        weaponStackMovementPercent =
            percent;
    }

    public float GetModuleEffectMultiplier(
    int slotIndex,
    ModuleDefinition module)
    {
        if (module == null)
            return 1f;

        if (slotIndex != 3)
            return 1f;

        if (module.exclusive)
            return 1f;

        if (IsModuleTypeCompatibleWithCurrentShip(module))
            return 2f;

        return 1f;
    }

    

    private float auraRangeBoostPercent;

    public float AuraRangeMultiplier =>
        1f + auraRangeBoostPercent / 100f;

    public void SetAuraRangeBoost(
        float percent)
    {
        if (!IsServer)
            return;

        auraRangeBoostPercent =
            Mathf.Max(
                0f,
                percent);
    }

    public void Heal(
    float amount)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        if (amount <= 0f)
            return;

        int healAmount =
            Mathf.Max(
                0,
                Mathf.RoundToInt(amount));

        if (healAmount <= 0)
            return;

        hp.Value =
            Mathf.Min(
                MaxHp,
                hp.Value + healAmount);
    }

    public void RestoreShield(
    float amount)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        // Statek nie posiada tarczy.
        if (MaxShield <= 0)
            return;

        if (amount <= 0f)
            return;

        int shieldAmount =
            Mathf.Max(
                0,
                Mathf.RoundToInt(amount));

        if (shieldAmount <= 0)
            return;

        shield.Value =
            Mathf.Min(
                MaxShield,
                shield.Value + shieldAmount);
    }

    public void RecalculateResearchStats()
    {
        if (!IsServer)
            return;

        int oldMaxHp =
            maxHp.Value;

        CalculateDeployedModuleStats();

        int hpDifference =
            maxHp.Value - oldMaxHp;

        // HP dostaje wzrost wynikaj¹cy z researchu.
        if (hpDifference > 0)
        {
            hp.Value =
                Mathf.Min(
                    maxHp.Value,
                    hp.Value + hpDifference);
        }
        else
        {
            hp.Value =
                Mathf.Min(
                    hp.Value,
                    maxHp.Value);
        }

        // Shield NIE dostaje dodatkowych punktów.
        // Zmienia siê wy³¹cznie jego maksimum.
        shield.Value =
            Mathf.Min(
                shield.Value,
                maxShield.Value);
    }

    private PlayerUpgradeStats FindPlayerUpgradeStats(
    ulong clientId)
    {
        PlayerUpgradeStats[] all =
            FindObjectsByType<PlayerUpgradeStats>(
                FindObjectsSortMode.None);

        foreach (PlayerUpgradeStats upgrades in all)
        {
            if (!upgrades.IsSpawned)
                continue;

            if (upgrades.OwnerClientId ==
                clientId)
            {
                return upgrades;
            }
        }

        return null;
    }

}

