using UnityEngine;

public enum ShipType
{
    Fighter,
    Utility,
    Miner
}

[CreateAssetMenu(fileName = "New Ship", menuName = "RTS/Ship")]
public class ShipDefinition : ScriptableObject
{
    [Header("General")]
    public string shipId;
    public string displayName;

    public Sprite icon;

    public ShipType shipType;

    [Header("Prefab")]
    public GameObject shipPrefab;

    [Header("Build")]
    public int metalCost;
    public int energyCost;
    public float buildTime;

    [Header("Stats")]
    public int maxHp = 100;
    public float moveSpeed = 5;
    public int attackDamage = 10;
    public float attackRange = 5;
    public float attackCooldown = 1;
    public float minerSpeed = 1;

    [Header("Module Slots")]
    public int tier1Slots = 2;
    public int tier2Slots = 1;
    public int tier3Slots = 1;
}