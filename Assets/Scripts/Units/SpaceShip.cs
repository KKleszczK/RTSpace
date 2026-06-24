using Unity.Netcode;

public class SpaceShip : NetworkBehaviour
{
    public UnitOwner Owner { get; private set; }
    public NetworkHealth Health { get; private set; }
    public UnitMovement Movement { get; private set; }
    public UnitAttack Attack { get; private set; }
    public SelectionTarget Selection { get; private set; }

    public override void OnNetworkSpawn()
    {
        Owner = GetComponent<UnitOwner>();
        Health = GetComponent<NetworkHealth>();
        Movement = GetComponent<UnitMovement>();
        Attack = GetComponent<UnitAttack>();
        Selection = GetComponent<SelectionTarget>();
    }
}