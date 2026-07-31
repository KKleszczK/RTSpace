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
    public int maxShield = 0;
    public float moveSpeed = 5;
    
    

  
}