using Unity.Netcode;

public class PlayerResources : NetworkBehaviour
{
    public NetworkVariable<int> metal = new(500);
    public NetworkVariable<int> energy = new(250);

    public bool CanAfford(int metalCost, int energyCost)
    {
        return metal.Value >= metalCost && energy.Value >= energyCost;
    }

    public void Spend(int metalCost, int energyCost)
    {
        if (!IsServer) return;
        if (!CanAfford(metalCost, energyCost)) return;

        metal.Value -= metalCost;
        energy.Value -= energyCost;
    }

    public void AddMetal(int amount)
    {
        if (!IsServer) return;
        metal.Value += amount;
    }

    public void AddEnergy(int amount)
    {
        if (!IsServer) return;
        energy.Value += amount;
    }
}