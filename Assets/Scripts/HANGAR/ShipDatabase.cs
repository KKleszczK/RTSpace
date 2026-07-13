using System.Collections.Generic;
using UnityEngine;

public class ShipDatabase : MonoBehaviour
{
    public static ShipDatabase Instance { get; private set; }

    [SerializeField] private List<ShipDefinition> ships = new();

    private Dictionary<string, ShipDefinition> lookup;

    private void Awake()
    {
        Instance = this;

        lookup = new Dictionary<string, ShipDefinition>();

        foreach (ShipDefinition ship in ships)
        {
            lookup[ship.shipId] = ship;
        }
    }

    public ShipDefinition GetShip(string shipId)
    {
        lookup.TryGetValue(shipId, out ShipDefinition ship);
        return ship;
    }
}