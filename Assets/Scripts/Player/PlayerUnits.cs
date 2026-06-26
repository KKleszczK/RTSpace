using Unity.Netcode;

public class PlayerUnits : NetworkBehaviour
{
    public NetworkVariable<int> currentUnits = new(0);
    public NetworkVariable<int> maxUnits = new(30);

    public bool CanAddUnit()
    {
        return currentUnits.Value < maxUnits.Value;
    }

    public void AddUnit()
    {
        if (!IsServer) return;
        if (!CanAddUnit()) return;

        currentUnits.Value++;
    }

    public void RemoveUnit()
    {
        if (!IsServer) return;

        currentUnits.Value--;

        if (currentUnits.Value < 0)
            currentUnits.Value = 0;
    }
}