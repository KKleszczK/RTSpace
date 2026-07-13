using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseHangar : NetworkBehaviour
{
    public const int MaxDockedShips = 6;
    public const int MaxQueue = 2;
    public const int ModuleSlotCount = 4;

    public NetworkList<FixedString64Bytes> buildQueue;
    public NetworkList<DockedShipData> dockedShips;

    public NetworkVariable<float> buildProgress = new(0f);

    private ShipDefinition currentShip;

    private void Awake()
    {
        buildQueue = new NetworkList<FixedString64Bytes>();
        dockedShips = new NetworkList<DockedShipData>();
    }

    private void Update()
    {
        if (!IsServer)
            return;

        ProcessBuildQueue();
    }

    // =========================================================
    // BUILD SHIP
    // =========================================================

    public void RequestBuildShip(ShipDefinition ship)
    {
        if (ship == null)
            return;

        RequestBuildShipServerRpc(ship.shipId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildShipServerRpc(
        string shipId,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!CanClientUseHangar(senderId))
            return;

        if (buildQueue.Count >= MaxQueue)
            return;

        ShipDefinition ship = ShipDatabase.Instance.GetShip(shipId);

        if (ship == null)
            return;

        PlayerResources resources =
            FindPlayerResources(senderId);

        if (resources == null)
            return;

        if (!resources.CanAfford(
                ship.metalCost,
                ship.energyCost))
        {
            return;
        }

        resources.Spend(
            ship.metalCost,
            ship.energyCost);

        buildQueue.Add(
            new FixedString64Bytes(ship.shipId));
    }

    private void ProcessBuildQueue()
    {
        if (buildQueue.Count == 0)
        {
            currentShip = null;
            buildProgress.Value = 0f;
            return;
        }

        if (currentShip == null)
        {
            string shipId =
                buildQueue[0].ToString();

            currentShip =
                ShipDatabase.Instance.GetShip(shipId);
        }

        if (currentShip == null)
        {
            buildQueue.RemoveAt(0);
            buildProgress.Value = 0f;
            return;
        }

        float buildTime =
            Mathf.Max(0.01f, currentShip.buildTime);

        buildProgress.Value +=
            Time.deltaTime / buildTime;

        if (buildProgress.Value < 1f)
            return;

        buildProgress.Value = 1f;

        // Statek czeka na 100%, gdy hangar jest pe³ny.
        if (dockedShips.Count >= MaxDockedShips)
            return;

        DockedShipData newShip =
            new DockedShipData
            {
                instanceId =
                    new FixedString64Bytes(
                        Guid.NewGuid().ToString()),

                shipId =
                    new FixedString64Bytes(
                        currentShip.shipId),

                module1 = default,
                module2 = default,
                module3 = default,
                classModule = default
            };

        dockedShips.Add(newShip);
        buildQueue.RemoveAt(0);

        currentShip = null;
        buildProgress.Value = 0f;
    }

    public void RequestRemoveFromQueue(int index)
    {
        RemoveFromQueueServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveFromQueueServerRpc(
        int index,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId =
            rpcParams.Receive.SenderClientId;

        if (!CanClientUseHangar(senderId))
            return;

        if (index < 0 ||
            index >= buildQueue.Count)
        {
            return;
        }

        string shipId =
            buildQueue[index].ToString();

        ShipDefinition ship =
            ShipDatabase.Instance.GetShip(shipId);

        PlayerResources resources =
            FindPlayerResources(senderId);

        if (ship != null && resources != null)
        {
            resources.AddMetal(ship.metalCost);
            resources.AddEnergy(ship.energyCost);
        }

        buildQueue.RemoveAt(index);

        if (index == 0)
        {
            currentShip = null;
            buildProgress.Value = 0f;
        }
    }

    // =========================================================
    // INSTALL MODULE
    // =========================================================

    public void RequestInstallModule(
        int dockIndex,
        int slotIndex,
        string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId))
            return;

        InstallModuleServerRpc(
            dockIndex,
            slotIndex,
            moduleId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void InstallModuleServerRpc(
        int dockIndex,
        int slotIndex,
        string moduleId,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId =
            rpcParams.Receive.SenderClientId;

        if (!CanClientUseHangar(senderId))
            return;

        if (!IsValidDockIndex(dockIndex))
            return;

        if (!IsValidModuleSlot(slotIndex))
            return;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(moduleId);

        if (module == null)
            return;

        PlayerModuleInventory inventory =
            FindPlayerInventory(senderId);

        if (inventory == null)
            return;

        if (!inventory.HasModule(moduleId))
            return;

        DockedShipData shipData =
            dockedShips[dockIndex];

        ShipDefinition shipDefinition =
            ShipDatabase.Instance.GetShip(
                shipData.shipId.ToString());

        if (shipDefinition == null)
            return;

        if (!CanInstallInSlot(
                module,
                shipDefinition,
                slotIndex))
        {
            return;
        }

        if (shipData.CountModule(moduleId) >=
            Mathf.Max(1, module.maxCopiesPerPlayer))
        {
            return;
        }

        FixedString64Bytes oldModuleId =
            shipData.GetModule(slotIndex);

        // Je¿eli w slocie by³ modu³, zwracamy go.
        if (!oldModuleId.IsEmpty)
        {
            inventory.AddModule(
                oldModuleId.ToString());
        }

        if (!inventory.RemoveOneModule(moduleId))
            return;

        shipData.SetModule(
            slotIndex,
            new FixedString64Bytes(moduleId));

        // NetworkList wymaga ponownego wpisania struktury.
        dockedShips[dockIndex] = shipData;
    }

    // =========================================================
    // REMOVE MODULE
    // =========================================================

    public void RequestRemoveModule(
        int dockIndex,
        int slotIndex)
    {
        RemoveModuleServerRpc(
            dockIndex,
            slotIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveModuleServerRpc(
        int dockIndex,
        int slotIndex,
        ServerRpcParams rpcParams = default)
    {
        ulong senderId =
            rpcParams.Receive.SenderClientId;

        if (!CanClientUseHangar(senderId))
            return;

        if (!IsValidDockIndex(dockIndex))
            return;

        if (!IsValidModuleSlot(slotIndex))
            return;

        DockedShipData shipData =
            dockedShips[dockIndex];

        FixedString64Bytes moduleId =
            shipData.GetModule(slotIndex);

        if (moduleId.IsEmpty)
            return;

        PlayerModuleInventory inventory =
            FindPlayerInventory(senderId);

        if (inventory == null)
            return;

        inventory.AddModule(moduleId.ToString());

        shipData.SetModule(slotIndex, default);
        dockedShips[dockIndex] = shipData;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool CanInstallInSlot(
        ModuleDefinition module,
        ShipDefinition ship,
        int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex <= 2)
        {
            // Zak³adam:
            // Tier1 = 0, Tier2 = 1, Tier3 = 2.
            return (int)module.tier == slotIndex;
        }

        if (slotIndex == 3)
        {
            // Zak³adam zgodn¹ kolejnoœæ:
            // Miner, Fighter, Utility lub podobn¹.
            return (int)module.type ==
                   (int)ship.shipType;
        }

        return false;
    }

    private bool IsValidDockIndex(int index)
    {
        return index >= 0 &&
               index < dockedShips.Count;
    }

    private bool IsValidModuleSlot(int index)
    {
        return index >= 0 &&
               index < ModuleSlotCount;
    }

    private bool CanClientUseHangar(ulong clientId)
    {
        return clientId == OwnerClientId;
    }

    private PlayerResources FindPlayerResources(
        ulong clientId)
    {
        PlayerResources[] players =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        foreach (PlayerResources player in players)
        {
            if (player.OwnerClientId == clientId)
                return player;
        }

        return null;
    }

    private PlayerModuleInventory FindPlayerInventory(
        ulong clientId)
    {
        PlayerModuleInventory[] inventories =
            FindObjectsByType<PlayerModuleInventory>(
                FindObjectsSortMode.None);

        foreach (PlayerModuleInventory inventory
                 in inventories)
        {
            if (inventory.OwnerClientId == clientId)
                return inventory;
        }

        return null;
    }
}