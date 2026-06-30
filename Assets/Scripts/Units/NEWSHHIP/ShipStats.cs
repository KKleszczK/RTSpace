using UnityEngine;

public class ShipStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private int baseMaxHp = 100;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private int baseAttackDamage = 10;
    [SerializeField] private float baseAttackRange = 5f;
    [SerializeField] private float baseAttackCooldown = 1f;

    public int MaxHp { get; private set; }
    public float MoveSpeed { get; private set; }
    public int AttackDamage { get; private set; }
    public float AttackRange { get; private set; }
    public float AttackCooldown { get; private set; }



    private void Awake()
    {
        MaxHp = baseMaxHp;
        MoveSpeed = baseMoveSpeed;
        AttackDamage = baseAttackDamage;
        AttackRange = baseAttackRange;
        AttackCooldown = baseAttackCooldown;
    }

    public void RecalculateStats()
    {
        PlayerUpgradeStats upgrades = GetPlayerUpgrades();

        if (upgrades == null)
        {
            MaxHp = baseMaxHp;
            MoveSpeed = baseMoveSpeed;
            AttackDamage = baseAttackDamage;
            AttackRange = baseAttackRange;
            AttackCooldown = baseAttackCooldown;
            return;
        }

        MaxHp = Mathf.RoundToInt(baseMaxHp * (1f + upgrades.shipHpBonusPercent.Value));

        // Na razie pozosta³e statystyki bez bonusów
        MoveSpeed = baseMoveSpeed;
        AttackDamage = baseAttackDamage;
        AttackRange = baseAttackRange;
        AttackCooldown = baseAttackCooldown;
        Debug.Log("SHIP MAX HP: " + MaxHp);
    }

    private PlayerUpgradeStats GetPlayerUpgrades()
    {
        UnitOwner owner = GetComponent<UnitOwner>();

        if (owner == null)
            return null;

        PlayerUpgradeStats[] all =
            FindObjectsByType<PlayerUpgradeStats>(FindObjectsSortMode.None);

        foreach (PlayerUpgradeStats stats in all)
        {
            if (stats.OwnerClientId == owner.ownerId.Value)
                return stats;
        }

        return null;
    }
}