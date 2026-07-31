using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class ShipUnit : NetworkBehaviour
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
    [SerializeField] private int maxHp = 0;
    [SerializeField] private int maxShield = 0;
    [SerializeField] private float moveSpeed = 0f;

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

    public int MaxHp => maxHp;
    public int MaxShield => maxShield;
    public float MoveSpeed => moveSpeed;

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

        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsServer)
            return;

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

        if (definition != null)
        {
            baseMaxHp = definition.maxHp;
            baseMaxShield = definition.maxShield;
            baseMoveSpeed = definition.moveSpeed;
        }

        CalculateDeployedModuleStats();

        hp.Value =
            maxHp;

        shield.Value =
            maxShield;

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
            targetPosition.Value - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        RotateTowardsMovement(direction);

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                targetPosition.Value,
                moveSpeed * Time.deltaTime);
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

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(
        int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(
        int damage)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        int remainingDamage =
            damage;

        if (shield.Value > 0)
        {
            int shieldDamage =
                Mathf.Min(
                    shield.Value,
                    remainingDamage);

            shield.Value -=
                shieldDamage;

            remainingDamage -=
                shieldDamage;
        }

        if (remainingDamage > 0)
        {
            hp.Value -=
                remainingDamage;
        }

        if (hp.Value <= 0)
        {
            hp.Value = 0;
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        isDead.Value =
            true;

        NetworkObject.Despawn(true);
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

    private void UpdateHpBar()
    {
        if (currentHpBar != null)
        {
            float percent =
                maxHp > 0
                    ? (float)hp.Value / maxHp
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
                $"{hp.Value}/{maxHp}";
        }
    }

    private void UpdateShieldBar()
    {
        if (currentShieldBar != null)
        {
            float percent =
                maxShield > 0
                    ? (float)shield.Value / maxShield
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
                $"{shield.Value}/{maxShield}";
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
            ref totals);

        AddModuleStats(
            normalModule2.Value,
            ref totals);

        AddModuleStats(
            normalModule3.Value,
            ref totals);

        AddModuleStats(
            classModule.Value,
            ref totals);

        float calculatedMaxHp =
            (baseMaxHp + totals.HpFlat) *
            (1f + totals.HpPercent / 100f);

        float calculatedMaxShield =
            (baseMaxShield + totals.ShieldFlat) *
            (1f + totals.ShieldPercent / 100f);

        float calculatedMoveSpeed =
            (baseMoveSpeed + totals.MoveSpeedFlat) *
            (1f + totals.MoveSpeedPercent / 100f);

        maxHp =
            Mathf.Max(
                1,
                Mathf.RoundToInt(calculatedMaxHp));

        maxShield =
            Mathf.Max(
                0,
                Mathf.RoundToInt(calculatedMaxShield));

        moveSpeed =
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
            $"HP={maxHp}, " +
            $"Shield={maxShield}, " +
            $"Speed={moveSpeed:0.##}, " +
            $"Damage x{weaponsDamageMultiplier:0.##}, " +
            $"AttackSpeed x{weaponsAttackSpeedMultiplier:0.##}",
            this);
    }

    private void AddModuleStats(
    FixedString64Bytes moduleId,
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

        totals.Add(module);
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

        public void Add(ModuleDefinition module)
        {
            if (module == null)
                return;

            ShieldFlat +=
                module.shieldFlat;

            ShieldPercent +=
                module.shieldPercent;

            HpFlat +=
                module.hpFlat;

            HpPercent +=
                module.hpPercent;

            MoveSpeedFlat +=
                module.moveSpeedFlat;

            MoveSpeedPercent +=
                module.moveSpeedPercent;

            WeaponsDamagePercent +=
                module.allWeaponsDamagePercent;

            WeaponsAttackSpeedPercent +=
                module.allWeaponsAttackSpeedPercent;
        }
    }

}

