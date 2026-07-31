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

    [Header("Ship Deploy")]
    [SerializeField] private Transform launchCenter;

    [SerializeField, Min(1f)]
    private float launchRadius = 8f;

    [SerializeField]
    private float launchHeight = 0.5f;

    [SerializeField]
    private bool faceAwayFromBase = true;

    [Header("Ship Docking")]
    [SerializeField, Min(1f)]
    private float dockingRange = 4f;

    private void Awake()
    {
        buildQueue = new NetworkList<FixedString64Bytes>();
        dockedShips = new NetworkList<DockedShipData>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            $"[HANGAR SPAWN] " +
            $"name={gameObject.name}, " +
            $"NetworkObjectId={NetworkObjectId}, " +
            $"OwnerClientId={OwnerClientId}, " +
            $"LocalClientId={NetworkManager.Singleton.LocalClientId}, " +
            $"IsOwner={IsOwner}, " +
            $"IsServer={IsServer}");
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
        Debug.Log(
            $"[SHIP BUILD 01] Klikniêto budowê. " +
            $"ship={(ship != null ? ship.shipId : "NULL")}, " +
            $"hangarOwner={OwnerClientId}, " +
            $"localClient={NetworkManager.Singleton.LocalClientId}, " +
            $"IsOwner={IsOwner}");

        if (ship == null)
        {
            Debug.LogWarning(
                "[SHIP BUILD BLOCKED 01] ShipDefinition == null.");

            return;
        }

        if (string.IsNullOrWhiteSpace(ship.shipId))
        {
            Debug.LogWarning(
                "[SHIP BUILD BLOCKED 02] shipId jest pusty.");

            return;
        }

        RequestBuildShipServerRpc(ship.shipId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestBuildShipServerRpc(
        string shipId,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[SHIP BUILD 02] ServerRpc odebrane. " +
            $"ship={shipId}, " +
            $"sender={senderClientId}, " +
            $"hangarOwner={OwnerClientId}, " +
            $"queue={buildQueue.Count}/{MaxQueue}, " +
            $"docked={dockedShips.Count}/{MaxDockedShips}");

        if (!CanUseHangar(senderClientId))
        {
            Debug.LogWarning(
                $"[SHIP BUILD BLOCKED 03] Gracz nie jest w³aœcicielem hangaru. " +
                $"sender={senderClientId}, " +
                $"hangarOwner={OwnerClientId}");

            return;
        }

        if (buildQueue.Count >= MaxQueue)
        {
            Debug.LogWarning(
                $"[SHIP BUILD BLOCKED 04] Kolejka jest pe³na. " +
                $"queue={buildQueue.Count}/{MaxQueue}");

            return;
        }

        if (ShipDatabase.Instance == null)
        {
            Debug.LogError(
                "[SHIP BUILD BLOCKED 05] ShipDatabase.Instance == null.");

            return;
        }

        ShipDefinition ship =
            ShipDatabase.Instance.GetShip(shipId);

        if (ship == null)
        {
            Debug.LogWarning(
                $"[SHIP BUILD BLOCKED 06] Nie znaleziono statku: {shipId}");

            return;
        }

        PlayerResources resources =
            FindPlayerResources(senderClientId);

        if (resources == null)
        {
            Debug.LogWarning(
                $"[SHIP BUILD BLOCKED 07] Nie znaleziono PlayerResources. " +
                $"clientId={senderClientId}");

            DebugAllPlayerResources();

            return;
        }

        Debug.Log(
            $"[SHIP BUILD 03] Znaleziono zasoby. " +
            $"resourceOwner={resources.OwnerClientId}, " +
            $"metal={resources.metal.Value}, " +
            $"energy={resources.energy.Value}, " +
            $"costMetal={ship.metalCost}, " +
            $"costEnergy={ship.energyCost}");

        if (!resources.CanAfford(
                ship.metalCost,
                ship.energyCost))
        {
            Debug.LogWarning(
                $"[SHIP BUILD BLOCKED 08] Brak zasobów. " +
                $"metal={resources.metal.Value}/{ship.metalCost}, " +
                $"energy={resources.energy.Value}/{ship.energyCost}");

            return;
        }

        resources.Spend(
            ship.metalCost,
            ship.energyCost);

        buildQueue.Add(
            new FixedString64Bytes(shipId));

        Debug.Log(
            $"[SHIP BUILD 04] Dodano statek do kolejki. " +
            $"ship={shipId}, " +
            $"client={senderClientId}, " +
            $"queue={buildQueue.Count}/{MaxQueue}");
    }

    public bool IsShipInDockingRange(ShipUnit ship)
    {
        if (ship == null)
            return false;

        if (!IsSpawned)
            return false;

        if (!ship.IsSpawned)
            return false;

        if (ship.isDead.Value)
            return false;

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        // Lokalne sprawdzenie u¿ywane przez przycisk.
        if (!IsServer)
        {
            if (OwnerClientId != localClientId)
                return false;

            if (ship.ownerId.Value != localClientId)
                return false;
        }
        // Sprawdzenie serwerowe.
        else
        {
            if (ship.ownerId.Value != OwnerClientId)
                return false;
        }

        Vector3 hangarPosition =
            launchCenter != null
                ? launchCenter.position
                : transform.position;

        Vector3 shipPosition =
            ship.transform.position;

        hangarPosition.y = 0f;
        shipPosition.y = 0f;

        float distance =
            Vector3.Distance(
                hangarPosition,
                shipPosition);

        return distance <= dockingRange;
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

            Debug.Log(
                $"[SHIP BUILD PROCESS] Rozpoczêto budowê: {shipId}");
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

        if (dockedShips.Count >= MaxDockedShips)
        {
            Debug.LogWarning(
                $"[SHIP BUILD WAITING] Hangar jest pe³ny. " +
                $"docked={dockedShips.Count}/{MaxDockedShips}");

            return;
        }

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
            $"[HANGAR] Zbudowano statek: {currentShip.shipId}. " +
            $"OwnerClientId={OwnerClientId}");

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
        {
            Debug.LogWarning(
                $"[QUEUE REMOVE BLOCKED] sender={senderClientId}, " +
                $"hangarOwner={OwnerClientId}");

            return;
        }

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

        Debug.Log(
            $"[QUEUE REMOVE] Usuniêto statek {shipId} z kolejki.");
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
                $"[MODULE INSTALL ERROR] Gracz nie jest w³aœcicielem hangaru. " +
                $"sender={senderClientId}, owner={OwnerClientId}");

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

        if (slotIndex >= NormalSlot1 &&
            slotIndex <= NormalSlot3 &&
            slotIndex >= coreTier)
        {
            Debug.LogWarning(
                $"[MODULE INSTALL] Slot {slotIndex} zablokowany. " +
                $"Core tier={coreTier}");

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

        if (!inventory.RemoveOneModule(moduleId))
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Nie uda³o siê usun¹æ modu³u z inventory.");

            return;
        }

        if (!previousModuleId.IsEmpty)
        {
            inventory.AddModule(
                previousModuleId.ToString());
        }

        shipData.SetModule(
            slotIndex,
            new FixedString64Bytes(moduleId));

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

        if (module.exclusive && !isClassSlot)
        {
            Debug.LogWarning(
                "[MODULE INSTALL ERROR] Modu³ exclusive mo¿e byæ montowany " +
                "tylko w slocie klasowym.");

            return false;
        }

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
        {
            Debug.LogWarning(
                $"[MODULE REMOVE BLOCKED] sender={senderClientId}, " +
                $"hangarOwner={OwnerClientId}");

            return;
        }

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
     // DEPLOY SHIP
     // =========================================================

    public void RequestLaunchShip(int dockIndex)
    {
        if (!IsSpawned)
            return;

        LaunchShipServerRpc(dockIndex);
    }

    [ServerRpc(RequireOwnership = false)]
    private void LaunchShipServerRpc(
        int dockIndex,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[SHIP DEPLOY 01] " +
            $"dockIndex={dockIndex}, " +
            $"sender={senderClientId}, " +
            $"hangarOwner={OwnerClientId}");

        if (!CanUseHangar(senderClientId))
        {
            Debug.LogWarning(
                "[SHIP DEPLOY BLOCKED] Gracz nie jest w³aœcicielem hangaru.");

            return;
        }

        if (!IsValidDockIndex(dockIndex))
        {
            Debug.LogWarning(
                $"[SHIP DEPLOY BLOCKED] Nieprawid³owy dockIndex={dockIndex}.");

            return;
        }

        if (ShipDatabase.Instance == null)
        {
            Debug.LogError(
                "[SHIP DEPLOY BLOCKED] ShipDatabase.Instance == null.");

            return;
        }

        DockedShipData shipData =
            dockedShips[dockIndex];

        string shipId =
            shipData.shipId.ToString();

        ShipDefinition shipDefinition =
            ShipDatabase.Instance.GetShip(shipId);

        if (shipDefinition == null)
        {
            Debug.LogError(
                $"[SHIP DEPLOY BLOCKED] Nie znaleziono ShipDefinition: {shipId}");

            return;
        }

        if (shipDefinition.shipPrefab == null)
        {
            Debug.LogError(
                $"[SHIP DEPLOY BLOCKED] Statek {shipId} nie ma przypisanego prefaba.");

            return;
        }

        NetworkObject prefabNetworkObject =
            shipDefinition.shipPrefab.GetComponent<NetworkObject>();

        if (prefabNetworkObject == null)
        {
            Debug.LogError(
                $"[SHIP DEPLOY BLOCKED] Prefab {shipDefinition.shipPrefab.name} " +
                $"nie ma komponentu NetworkObject.");

            return;
        }

        ShipUnit prefabShipUnit =
            shipDefinition.shipPrefab.GetComponent<ShipUnit>();

        if (prefabShipUnit == null)
        {
            Debug.LogError(
                $"[SHIP DEPLOY BLOCKED] Prefab {shipDefinition.shipPrefab.name} " +
                $"nie ma komponentu ShipUnit.");

            return;
        }

        Vector3 spawnPosition =
            GetRandomLaunchPosition();

        Quaternion spawnRotation =
            GetLaunchRotation(spawnPosition);

        GameObject spawnedShip =
            Instantiate(
                shipDefinition.shipPrefab,
                spawnPosition,
                spawnRotation);

        NetworkObject networkObject =
            spawnedShip.GetComponent<NetworkObject>();

        ShipUnit shipUnit =
            spawnedShip.GetComponent<ShipUnit>();

        /*
         * SpawnWithOwnership ustawia w³aœciciela NetworkObject,
         * ale ShipUnit ma te¿ w³asne ownerId.
         */
        networkObject.SpawnWithOwnership(
            senderClientId);

        shipUnit.InitializeFromDockedShip(
            shipData,
            shipDefinition,
            senderClientId);

        dockedShips.RemoveAt(dockIndex);

        Debug.Log(
            $"[SHIP DEPLOY 02] Wypuszczono statek. " +
            $"shipId={shipId}, " +
            $"prefab={shipDefinition.shipPrefab.name}, " +
            $"owner={senderClientId}, " +
            $"position={spawnPosition}");
    }

    private Vector3 GetRandomLaunchPosition()
    {
        Transform center =
            launchCenter != null
                ? launchCenter
                : transform;

        float angle =
            UnityEngine.Random.Range(
                0f,
                Mathf.PI * 2f);

        Vector3 direction =
            new Vector3(
                Mathf.Cos(angle),
                0f,
                Mathf.Sin(angle));

        Vector3 position =
            center.position +
            direction * launchRadius;

        position.y =
            center.position.y +
            launchHeight;

        return position;
    }

    private Quaternion GetLaunchRotation(
        Vector3 spawnPosition)
    {
        Transform center =
            launchCenter != null
                ? launchCenter
                : transform;

        if (!faceAwayFromBase)
            return center.rotation;

        Vector3 direction =
            spawnPosition -
            center.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return center.rotation;

        return Quaternion.LookRotation(
            direction.normalized,
            Vector3.up);
    }
    // =========================================================
    // DOCK SHIP
    // =========================================================

    public bool HasFreeDockSlot()
    {
        return dockedShips != null &&
               dockedShips.Count < MaxDockedShips;
    }

    public void RequestDockShip(ShipUnit ship)
    {
        if (ship == null)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 01] ShipUnit == null.");

            return;
        }

        if (!IsSpawned)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 02] Hangar nie jest zespawnowany.");

            return;
        }

        if (!ship.IsSpawned)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 03] Statek nie jest zespawnowany.");

            return;
        }

        NetworkObject shipNetworkObject =
            ship.NetworkObject;

        if (shipNetworkObject == null)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 04] Statek nie ma NetworkObject.");

            return;
        }

        NetworkObjectReference shipReference =
            new NetworkObjectReference(shipNetworkObject);

        DockShipServerRpc(shipReference);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DockShipServerRpc(
        NetworkObjectReference shipReference,
        ServerRpcParams rpcParams = default)
    {
        ulong senderClientId =
            rpcParams.Receive.SenderClientId;

        Debug.Log(
            $"[SHIP DOCK 01] ¯¹danie dokowania. " +
            $"sender={senderClientId}, " +
            $"hangarOwner={OwnerClientId}, " +
            $"docked={dockedShips.Count}/{MaxDockedShips}");

        // Gracz mo¿e u¿ywaæ tylko w³asnego hangaru.
        if (!CanUseHangar(senderClientId))
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 05] Próba u¿ycia obcego hangaru. " +
                $"sender={senderClientId}, " +
                $"hangarOwner={OwnerClientId}");

            return;
        }

        // Sprawdzenie wolnego miejsca.
        if (dockedShips.Count >= MaxDockedShips)
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 06] Hangar jest pe³ny. " +
                $"docked={dockedShips.Count}/{MaxDockedShips}");

            return;
        }

        // Pobranie NetworkObject statku.
        if (!shipReference.TryGet(
                out NetworkObject shipNetworkObject))
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 07] Nie znaleziono NetworkObject statku.");

            return;
        }

        if (shipNetworkObject == null ||
            !shipNetworkObject.IsSpawned)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 08] Statek nie jest aktywnym NetworkObject.");

            return;
        }

        ShipUnit ship =
            shipNetworkObject.GetComponent<ShipUnit>();

        if (ship == null)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 09] Obiekt nie posiada ShipUnit.");

            return;
        }

        if (ship.isDead.Value)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 10] Nie mo¿na zadokowaæ zniszczonego statku.");

            return;
        }

        // Weryfikacja w³aœciciela zapisanego w ShipUnit.
        if (ship.ownerId.Value != senderClientId)
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 11] Statek nale¿y do innego gracza. " +
                $"shipOwner={ship.ownerId.Value}, " +
                $"sender={senderClientId}");

            return;
        }

        // Dodatkowa weryfikacja w³aœciciela NetworkObject.
        if (shipNetworkObject.OwnerClientId != senderClientId)
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 12] NetworkObject nale¿y do innego gracza. " +
                $"networkOwner={shipNetworkObject.OwnerClientId}, " +
                $"sender={senderClientId}");

            return;
        }

        // Serwer sam sprawdza odleg³oœæ.
        if (!IsShipInDockingRange(ship))
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 13] Statek jest poza zasiêgiem. " +
                $"ship={ship.gameObject.name}, " +
                $"range={dockingRange}");

            return;
        }

        if (ship.instanceId.Value.IsEmpty)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 14] Statek nie posiada instanceId.");

            return;
        }

        if (ship.shipId.Value.IsEmpty)
        {
            Debug.LogWarning(
                "[SHIP DOCK BLOCKED 15] Statek nie posiada shipId.");

            return;
        }

        // Zapisujemy statek razem z zamontowanymi modu³ami.
        DockedShipData dockedShipData =
            ship.CreateDockedShipData();

        // Zabezpieczenie przed podwójnym dodaniem tego samego statku.
        if (ContainsDockedShipInstance(
                dockedShipData.instanceId))
        {
            Debug.LogWarning(
                $"[SHIP DOCK BLOCKED 16] Statek instanceId=" +
                $"{dockedShipData.instanceId} ju¿ znajduje siê w hangarze.");

            return;
        }

        dockedShips.Add(
            dockedShipData);

        Debug.Log(
            $"[SHIP DOCK 02] Zadokowano statek. " +
            $"instanceId={dockedShipData.instanceId}, " +
            $"shipId={dockedShipData.shipId}, " +
            $"module1={dockedShipData.normalModule1}, " +
            $"module2={dockedShipData.normalModule2}, " +
            $"module3={dockedShipData.normalModule3}, " +
            $"classModule={dockedShipData.classModule}, " +
            $"docked={dockedShips.Count}/{MaxDockedShips}");

        shipNetworkObject.Despawn(true);
    }

    private bool ContainsDockedShipInstance(
        FixedString64Bytes instanceId)
    {
        for (int index = 0;
             index < dockedShips.Count;
             index++)
        {
            if (dockedShips[index].instanceId.Equals(
                    instanceId))
            {
                return true;
            }
        }

        return false;
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
            if (!player.IsSpawned)
                continue;

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
            if (!inventory.IsSpawned)
                continue;

            if (inventory.OwnerClientId == clientId)
                return inventory;
        }

        return null;
    }

    private BaseCore FindCoreForOwner(
        ulong clientId)
    {
        BaseCore[] cores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore core in cores)
        {
            if (!core.IsSpawned)
                continue;

            if (core.OwnerClientId == clientId)
                return core;
        }

        return null;
    }

    private void DebugAllPlayerResources()
    {
        PlayerResources[] allResources =
            FindObjectsByType<PlayerResources>(
                FindObjectsSortMode.None);

        Debug.Log(
            $"[RESOURCES DEBUG] Znaleziono obiektów: {allResources.Length}");

        foreach (PlayerResources resources in allResources)
        {
            Debug.Log(
                $"[RESOURCES DEBUG] " +
                $"name={resources.gameObject.name}, " +
                $"spawned={resources.IsSpawned}, " +
                $"owner={resources.OwnerClientId}, " +
                $"metal={resources.metal.Value}, " +
                $"energy={resources.energy.Value}");
        }
    }
}