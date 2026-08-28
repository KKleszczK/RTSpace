using Unity.Netcode;
using UnityEngine;

public class BaseUnit : NetworkBehaviour, IDamageable
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private UnitOwner owner;

    // =========================================================
    // BASE STATS
    // =========================================================

    [Header("Base Stats")]
    [SerializeField] private int baseMaxHp = 1000;
    [SerializeField] private int baseMaxShield = 0;

    // =========================================================
    // COMBAT FEEDBACK
    // =========================================================

    [Header("Combat Feedback")]
    [SerializeField] private Transform combatTextOrigin;
    [SerializeField] private CombatFloatingText combatTextPrefab;

    // =========================================================
    // NETWORK STATS
    // =========================================================

    public NetworkVariable<int> maxHp = new(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> hp = new(
        1000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> maxShield = new(
        0,
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

    // =========================================================
    // IDAMAGEABLE
    // =========================================================

    public ulong OwnerId =>
        owner != null
            ? owner.ownerId.Value
            : ulong.MaxValue;

    public bool IsDead =>
        isDead.Value;

    public Transform DamageTransform =>
        transform;

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

        RecalculateResearchStats(
            true);
    }

    // =========================================================
    // RESEARCH
    // =========================================================

    public void RecalculateResearchStats(
        bool fullRestore = false)
    {
        if (!IsServer)
            return;

        int oldMaxHp =
            maxHp.Value;

        PlayerUpgradeStats upgrades =
            FindPlayerUpgradeStats();

        float hpResearchFlat = 0f;
        float shieldResearchFlat = 0f;

        if (upgrades != null)
        {
            hpResearchFlat =
                upgrades.stationHpFlat.Value;

            shieldResearchFlat =
                upgrades.stationShieldFlat.Value;
        }

        maxHp.Value =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    baseMaxHp +
                    hpResearchFlat));

        maxShield.Value =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    baseMaxShield +
                    shieldResearchFlat));

        if (fullRestore)
        {
            hp.Value =
                maxHp.Value;

            shield.Value =
                maxShield.Value;

            return;
        }

        // HP research dodaje ró¿nicê od razu.
        int hpDifference =
            maxHp.Value -
            oldMaxHp;

        if (hpDifference > 0)
        {
            hp.Value =
                Mathf.Min(
                    maxHp.Value,
                    hp.Value +
                    hpDifference);
        }
        else
        {
            hp.Value =
                Mathf.Min(
                    hp.Value,
                    maxHp.Value);
        }

        /*
         * Analogicznie jak przy statkach:
         * research Shield zwiêksza maksimum,
         * ale NIE ³aduje aktualnej tarczy.
         */
        shield.Value =
            Mathf.Min(
                shield.Value,
                maxShield.Value);
    }

    private PlayerUpgradeStats
        FindPlayerUpgradeStats()
    {
        if (owner == null)
            return null;

        PlayerUpgradeStats[] all =
            FindObjectsByType<PlayerUpgradeStats>(
                FindObjectsSortMode.None);

        foreach (PlayerUpgradeStats upgrades in all)
        {
            if (!upgrades.IsSpawned)
                continue;

            if (upgrades.OwnerClientId ==
                owner.ownerId.Value)
            {
                return upgrades;
            }
        }

        return null;
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
        // SHIELD
        // =====================================================

        if (shield.Value > 0)
        {
            int requestedShieldDamage =
                Mathf.Max(
                    0,
                    Mathf.RoundToInt(
                        shieldDamage));

            int actualShieldDamage =
                Mathf.Min(
                    shield.Value,
                    requestedShieldDamage);

            shield.Value -=
                actualShieldDamage;

            ShowShieldDamageClientRpc(
                actualShieldDamage);

            Debug.Log(
                $"[BASE DAMAGE] " +
                $"Shield -{actualShieldDamage} | " +
                $"Shield={shield.Value}/{maxShield.Value}");

            /*
             * Bardzo wa¿ne:
             *
             * Je¿eli baza mia³a tarczê w momencie
             * trafienia, hit koñczy siê tutaj.
             *
             * Nadmiar damage NIE przechodzi na hull.
             */
            return;
        }

        // =====================================================
        // HULL
        // =====================================================

        int requestedHullDamage =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    hullDamage));

        int actualHullDamage =
            Mathf.Min(
                hp.Value,
                requestedHullDamage);

        hp.Value -=
            actualHullDamage;

        ShowHullDamageClientRpc(
            actualHullDamage);

        Debug.Log(
            $"[BASE DAMAGE] " +
            $"Hull -{actualHullDamage} | " +
            $"HP={hp.Value}/{maxHp.Value}");

        // =====================================================
        // DEATH
        // =====================================================

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
    // HEAL / SHIELD
    // =========================================================

    public void Heal(
        float amount)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        int finalAmount =
            Mathf.Max(
                0,
                Mathf.RoundToInt(amount));

        hp.Value =
            Mathf.Min(
                maxHp.Value,
                hp.Value +
                finalAmount);
    }

    public void RestoreShield(
        float amount)
    {
        if (!IsServer)
            return;

        if (isDead.Value)
            return;

        if (maxShield.Value <= 0)
            return;

        int finalAmount =
            Mathf.Max(
                0,
                Mathf.RoundToInt(amount));

        shield.Value =
            Mathf.Min(
                maxShield.Value,
                shield.Value +
                finalAmount);
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

        Debug.Log(
            $"[BASE DESTROYED] Owner={OwnerId}",
            this);

        /*
         * Na razie NIE despawnujemy bazy.
         *
         * PóŸniej tutaj podepniemy:
         * GameManager / WinCondition.
         */
    }

    
}