using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class BaseHangar : NetworkBehaviour
{
    public const int MaxDockedShips = 6;
    public const int MaxQueue = 2;

    private const int NormalSlot1 = 0;
    private const int NormalSlot2 = 1;
    private const int NormalSlot3 = 2;
    private const int ClassSlot = 3;

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

        if (string.IsNullOrWhiteSpace(ship.shipId))
            return;

        RequestBuildShipServerRpc(ship.shipId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildShipServerRpc(
        string shipId,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!CanUseHangar(senderClientId))
            return;

        if (buildQueue.Count >= MaxQueue)
            return;

        if (ShipDatabase.Instance == null)
            return;

        ShipDefinition ship =
            ShipDatabase.Instance.GetShip(shipId);

        if (ship == null)
            return;

        PlayerResources resources =
            FindPlayerResources(senderClientId);

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
            new FixedString64Bytes(shipId));
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
                ShipDatabase.Instance != null
                    ? ShipDatabase.Instance.GetShip(shipId)
                    : null;
        }

        if (currentShip == null)
        {
            Debug.LogError(
                "[HANGAR] Nie znaleziono statku z pocz¹tku kolejki.");

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

        // Przy pe³nym hangarze statek czeka na 100%.
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

                normalModule1 = default,
                normalModule2 = default,
                normalModule3 = default,
                classModule = default
            };

        dockedShips.Add(newShip);
        buildQueue.RemoveAt(0);

        Debug.Log(
            $"[HANGAR] Zbudowano statek: {currentShip.shipId}");

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
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!CanUseHangar(senderClientId))
            return;

        if (index < 0 || index >= buildQueue.Count)
            return;

        string shipId =
            buildQueue[index].ToString();

        ShipDefinition ship =
            ShipDatabase.Instance != null
                ? ShipDatabase.Instance.GetShip(shipId)
                : null;

        PlayerResources resources =
            FindPlayerResources(senderClientId);

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
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[MODULE INSTALL 01] " +
            $"dock={dockIndex}, slot={slotIndex}, module={moduleId}");

        if (!CanUseHangar(senderClientId))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Gracz nie jest w³aœcicielem hangaru.");

            return;
        }

        if (!IsValidDockIndex(dockIndex))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nieprawid³owy indeks statku.");

            return;
        }

        if (!IsValidSlotIndex(slotIndex))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nieprawid³owy indeks slotu.");

            return;
        }

        if (ModuleDatabase.Instance == null)
        {
            Debug.LogError(
                "[MODULE INSTALL ERROR] ModuleDatabase.Instance == null");

            return;
        }

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(moduleId);

        if (module == null)
        {
            Debug.LogWarning(
                $"[MODULE INSTALL ERROR] Nie znaleziono modu³u: {moduleId}");

            return;
        }

        BaseCore core =
            FindCoreForOwner(senderClientId);

        if (core == null)
        {
            Debug.LogWarning(
                "[MODULE INSTALL] Nie znaleziono BaseCore gracza.");

            return;
        }

        int coreTier = core.tier.Value;

        if (slotIndex >= 0 &&
            slotIndex <= 2 &&
            slotIndex >= coreTier)
        {
            Debug.LogWarning(
                $"[MODULE INSTALL] Slot {slotIndex} zablokowany. Core tier={coreTier}");

            return;
        }

        PlayerModuleInventory inventory =
            FindPlayerInventory(senderClientId);

        if (inventory == null)
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nie znaleziono inventory gracza.");

            return;
        }

        if (!inventory.HasModule(moduleId))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Gracz nie posiada modu³u.");

            return;
        }

        DockedShipData shipData =
            dockedShips[dockIndex];

        ShipDefinition shipDefinition =
            ShipDatabase.Instance != null
                ? ShipDatabase.Instance.GetShip(
                    shipData.shipId.ToString())
                : null;

        if (shipDefinition == null)
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nie znaleziono definicji statku.");

            return;
        }

        if (!CanInstallModule(
                module,
                shipDefinition,
                slotIndex))
        {
            return;
        }

        FixedString64Bytes previousModuleId =
            shipData.GetModule(slotIndex);

        // Najpierw zabieramy nowy modu³ z inventory.
        if (!inventory.RemoveOneModule(moduleId))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nie uda³o siê usun¹æ modu³u z inventory.");

            return;
        }

        // Je¿eli slot by³ zajêty, poprzedni modu³ wraca do inventory.
        if (!previousModuleId.IsEmpty)
        {
            inventory.AddModule(
                previousModuleId.ToString());
        }

        shipData.SetModule(
            slotIndex,
            new FixedString64Bytes(moduleId));

        // DockedShipData jest struktur¹, wiêc wpisujemy j¹ ponownie.
        dockedShips[dockIndex] = shipData;

        Debug.Log(
            $"[MODULE INSTALL 02] Zamontowano {moduleId} " +
            $"na statku {shipData.shipId}, slot={slotIndex}");
    }

    private bool CanInstallModule(
        ModuleDefinition module,
        ShipDefinition ship,
        int slotIndex)
    {
        bool isClassSlot =
            slotIndex == ClassSlot;

        // Modu³ exclusive mo¿e wejœæ wy³¹cznie do slotu klasowego.
        if (module.exclusive && !isClassSlot)
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Modu³ exclusive mo¿e byæ montowany tylko w slocie klasowym.");

            return false;
        }

        // Slot klasowy wymaga zgodnego typu.
        if (isClassSlot)
        {
            if (!IsModuleTypeCompatibleWithShip(module, ship))
            {
                Debug.LogWarning(
                    $"[MODULE INSTALL ERROR] Modu³ typu {module.type} " +
                    $"nie pasuje do statku typu {ship.shipType}.");

                return false;
            }

            return true;
        }

        // Sloty 0–2 s¹ zwyk³e i nie przyjmuj¹ exclusive.
        if (slotIndex == NormalSlot1 ||
            slotIndex == NormalSlot2 ||
            slotIndex == NormalSlot3)
        {
            return !module.exclusive;
        }

        return false;
    }

    private bool IsModuleTypeCompatibleWithShip(
        ModuleDefinition module,
        ShipDefinition ship)
    {
        // Zak³adamy te same nazwy:
        // Fighter, Utility, Miner.
        return string.Equals(
            module.type.ToString(),
            ship.shipType.ToString(),
            StringComparison.OrdinalIgnoreCase);
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
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        if (!CanUseHangar(senderClientId))
            return;

        if (!IsValidDockIndex(dockIndex))
            return;

        if (!IsValidSlotIndex(slotIndex))
            return;

        DockedShipData shipData =
            dockedShips[dockIndex];

        FixedString64Bytes moduleId =
            shipData.GetModule(slotIndex);

        if (moduleId.IsEmpty)
            return;

        PlayerModuleInventory inventory =
            FindPlayerInventory(senderClientId);

        if (inventory == null)
            return;

        inventory.AddModule(moduleId.ToString());

        shipData.ClearModule(slotIndex);
        dockedShips[dockIndex] = shipData;

        Debug.Log(
            $"[MODULE REMOVE] Zdjêto {moduleId} " +
            $"ze statku {shipData.shipId}, slot={slotIndex}");
    }

    // =========================================================
    // VALIDATION / FIND
    // =========================================================

    private bool IsValidDockIndex(int index)
    {
        return index >= 0 &&
               index < dockedShips.Count;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= NormalSlot1 &&
               index <= ClassSlot;
    }

    private bool CanUseHangar(ulong clientId)
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

        foreach (PlayerModuleInventory inventory in inventories)
        {
            if (inventory.OwnerClientId == clientId)
                return inventory;
        }

        return null;
    }

    private BaseCore FindCoreForOwner(ulong clientId)
    {
        BaseCore[] cores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore core in cores)
        {
            if (core.OwnerClientId == clientId)
                return core;
        }

        return null;
    }
}