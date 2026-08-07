using Unity.Netcode;

public class PlayerUnits : NetworkBehaviour
{
    public NetworkVariable<int> currentUnits = new(0);
    public NetworkVariable<int> maxUnits = new(30);

    public bool CanReserveUnit()
    {
        return currentUnits.Value < maxUnits.Value;
    }

    public bool TryReserveUnit()
    {
        if (!IsServer)
            return false;

        if (!CanReserveUnit())
            return false;

        currentUnits.Value++;
        return true;
    }

    public void ReleaseUnit()
    {
        if (!IsServer)
            return;

        currentUnits.Value =
            System.Math.Max(
                0,
                currentUnits.Value - 1);
    }
}