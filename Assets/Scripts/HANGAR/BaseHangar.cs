using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseHangar : NetworkBehaviour
{
    public const int MaxDockedShips = 6;
    public const int MaxQueue = 2;

    public NetworkList<FixedString64Bytes> buildQueue;
    public NetworkList<FixedString64Bytes> dockedShips;

    public NetworkVariable<float> buildProgress = new(0f);

    private ShipDefinition currentShip;

    private void Awake()
    {
        buildQueue = new NetworkList<FixedString64Bytes>();
        dockedShips = new NetworkList<FixedString64Bytes>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        ProcessBuildQueue();
    }

    public void RequestBuildShip(ShipDefinition ship)
    {
        if (ship == null)
            return;

        RequestBuildShipServerRpc(ship.shipId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildShipServerRpc(string shipId, ServerRpcParams rpcParams = default)
    {
        if (buildQueue.Count >= MaxQueue)
            return;

        ShipDefinition ship = ShipDatabase.Instance.GetShip(shipId);

        if (ship == null)
            return;

        PlayerResources resources = FindPlayerResources(rpcParams.Receive.SenderClientId);

        if (resources == null)
            return;

        if (!resources.CanAfford(ship.metalCost, ship.energyCost))
            return;

        resources.Spend(ship.metalCost, ship.energyCost);

        buildQueue.Add(new FixedString64Bytes(shipId));
    }

    private void ProcessBuildQueue()
    {
        if (buildQueue.Count == 0)
        {
            currentShip = null;
            buildProgress.Value = 0f;
            return;
        }

        if (dockedShips.Count >= MaxDockedShips)
            return;

        if (currentShip == null)
        {
            string id = buildQueue[0].ToString();
            currentShip = ShipDatabase.Instance.GetShip(id);
        }

        if (currentShip == null)
            return;

        buildProgress.Value += Time.deltaTime / currentShip.buildTime;

        if (buildProgress.Value >= 1f)
        {
            dockedShips.Add(new FixedString64Bytes(currentShip.shipId));

            buildQueue.RemoveAt(0);
            currentShip = null;
            buildProgress.Value = 0f;
        }
    }

    public void RequestRemoveFromQueue(int index)
    {
        RemoveFromQueueServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveFromQueueServerRpc(int index)
    {
        if (index < 0 || index >= buildQueue.Count)
            return;

        string id = buildQueue[index].ToString();
        ShipDefinition ship = ShipDatabase.Instance.GetShip(id);

        if (ship != null)
        {
            PlayerResources resources = FindPlayerResources(OwnerClientId);

            if (resources != null)
            {
                resources.AddMetal(ship.metalCost);
                resources.AddEnergy(ship.energyCost);
            }
        }

        buildQueue.RemoveAt(index);

        if (index == 0)
        {
            currentShip = null;
            buildProgress.Value = 0f;
        }
    }

    private PlayerResources FindPlayerResources(ulong clientId)
    {
        PlayerResources[] all = FindObjectsByType<PlayerResources>(FindObjectsSortMode.None);

        foreach (PlayerResources r in all)
        {
            if (r.OwnerClientId == clientId)
                return r;
        }

        return null;
    }
}