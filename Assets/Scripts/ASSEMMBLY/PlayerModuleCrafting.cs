using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerModuleCrafting : NetworkBehaviour
{
    public const int MaxQueue = 5;

    public NetworkList<FixedString64Bytes> moduleQueue;

    public NetworkVariable<FixedString64Bytes> currentModuleId = new();
    public NetworkVariable<float> currentProgress = new(0f);

    private PlayerResources resources;
    private PlayerModuleInventory inventory;
    private ModuleDefinition currentModule;
    private PlayerResearch playerResearch;

    private BaseSafeZone safeZone;

    private void Awake()
    {
        resources =
            GetComponent<PlayerResources>();

        inventory =
            GetComponent<PlayerModuleInventory>();

        playerResearch =
            GetComponent<PlayerResearch>();

        moduleQueue =
            new NetworkList<FixedString64Bytes>();

        if (resources == null)
            Debug.LogError(
                "[CRAFT ERROR] Brak PlayerResources na PlayerPrefab",
                gameObject);

        if (inventory == null)
            Debug.LogError(
                "[CRAFT ERROR] Brak PlayerModuleInventory na PlayerPrefab",
                gameObject);

        if (playerResearch == null)
            Debug.LogError(
                "[CRAFT ERROR] Brak PlayerResearch na PlayerPrefab",
                gameObject);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        if (safeZone == null)
            FindSafeZone();

        ProcessQueue();
    }

    private void FindSafeZone()
    {
        BaseSafeZone[] safeZones =
            FindObjectsByType<BaseSafeZone>(
                FindObjectsSortMode.None);

        foreach (BaseSafeZone zone in safeZones)
        {
            if (!zone.IsSpawned)
                continue;

            UnitOwner baseOwner =
                zone.GetComponent<UnitOwner>();

            if (baseOwner == null)
                continue;

            if (baseOwner.ownerId.Value !=
                OwnerClientId)
            {
                continue;
            }

            safeZone = zone;
            return;
        }
    }

    public void RequestCraft(ModuleDefinition module)
    {
        Debug.Log(
            $"[CRAFT 04] RequestCraft | " +
            $"IsSpawned={IsSpawned} | " +
            $"IsClient={IsClient} | " +
            $"IsServer={IsServer} | " +
            $"IsOwner={IsOwner} | " +
            $"OwnerClientId={OwnerClientId}");

        if (module == null)
        {
            Debug.LogError("[CRAFT ERROR] module == null");
            return;
        }

        if (!IsSpawned)
        {
            Debug.LogError("[CRAFT ERROR] Obiekt nie jest zespawnowany");
            return;
        }

        RequestCraftServerRpc(module.moduleId);

        Debug.Log("[CRAFT 04B] ServerRpc zosta³ wys³any");
    }



    [ServerRpc(RequireOwnership = false)]
    private void RequestCraftServerRpc(string moduleId)
    {
        Debug.Log("[CRAFT 05] ServerRpc odebra³ modu³: " + moduleId);

        if (ModuleDatabase.Instance == null)
        {
            Debug.LogError("[CRAFT ERROR] ModuleDatabase.Instance == null");
            return;
        }

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(moduleId);

        if (module == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] Nie znaleziono modu³u w ModuleDatabase: " +
                moduleId);

            return;
        }

        // =========================================================
        // RESEARCH UNLOCK
        // =========================================================

        if (playerResearch == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] playerResearch == null");

            return;
        }

        if (!playerResearch.IsModuleUnlocked(module))
        {
            Debug.LogWarning(
                $"[CRAFT BLOCKED] Modu³ {module.moduleId} " +
                $"nie zosta³ jeszcze odblokowany.");

            return;
        }

        // =========================================================
        // CORE TIER
        // =========================================================

        BaseCore core =
            FindOwnedCore();

        if (core == null)
        {
            Debug.LogError(
                "[CRAFT ERROR] Nie znaleziono Core gracza.");

            return;
        }

        if ((int)module.tier > core.tier.Value)
        {
            Debug.LogWarning(
                $"[CRAFT BLOCKED] Modu³ {module.moduleId} " +
                $"wymaga Core Tier {(int)module.tier}, " +
                $"a gracz ma Tier {core.tier.Value}.");

            return;
        }

        // =========================================================
        // MAX COPIES PER PLAYER
        // =========================================================

        if (module.maxCopiesPerPlayer > 0)
        {
            int currentCopies =
                GetTotalModuleCopies(
                    module.moduleId);

            if (currentCopies >=
                module.maxCopiesPerPlayer)
            {
                Debug.LogWarning(
                    $"[CRAFT BLOCKED] " +
                    $"Module={module.moduleId} | " +
                    $"Copies={currentCopies}/" +
                    $"{module.maxCopiesPerPlayer}");

                return;
            }
        }

        Debug.Log(
            "[CRAFT 06] Parametry modu³u: " +
            $"M={module.metalCost}, " +
            $"E={module.energyCost}, " +
            $"T={module.craftTime}");

        if (moduleQueue.Count >= MaxQueue)
        {
            Debug.LogError("[CRAFT ERROR] Kolejka jest pe³na");
            return;
        }

        if (resources == null)
        {
            Debug.LogError("[CRAFT ERROR] resources == null");
            return;
        }

        if (!resources.CanAfford(
                module.metalCost,
                module.energyCost))
        {
            Debug.LogError("[CRAFT ERROR] Brak zasobów");
            return;
        }

        resources.Spend(
            module.metalCost,
            module.energyCost);

        moduleQueue.Add(
            new FixedString64Bytes(moduleId));

        Debug.Log(
            "[CRAFT 07] Dodano modu³ do kolejki. Count=" +
            moduleQueue.Count);
    }



    public void RequestRemoveFromQueue(int index)
    {
        RemoveFromQueueServerRpc(index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemoveFromQueueServerRpc(int index)
    {
        if (index < 0 || index >= moduleQueue.Count)
            return;

        string id = moduleQueue[index].ToString();

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(id);

        if (module != null && resources != null)
        {
            resources.AddMetal(module.metalCost);
            resources.AddEnergy(module.energyCost);
        }

        moduleQueue.RemoveAt(index);

        if (index == 0)
        {
            currentModule = null;
            currentModuleId.Value = default;
            currentProgress.Value = 0f;
        }
    }

    private void ProcessQueue()
    {
        if (moduleQueue.Count == 0)
        {
            currentModule = null;
            currentModuleId.Value = default;
            currentProgress.Value = 0f;
            return;
        }

        if (currentModule == null)
        {
            string moduleId =
                moduleQueue[0].ToString();

            currentModule =
                ModuleDatabase.Instance.GetModule(moduleId);

            currentModuleId.Value =
                moduleQueue[0];

            Debug.Log(
                "[CRAFT 08] Rozpoczêto crafting: " +
                moduleId);
        }

        if (currentModule == null)
        {
            Debug.LogError("[CRAFT ERROR] currentModule == null");
            return;
        }

        float baseCraftTime =
            Mathf.Max(
            0f,
        currentModule.craftTime);

        float assemblyBonusPercent =
            safeZone != null
                ? safeZone.GetAssemblySpeedBonusPercent()
                : 0f;

        assemblyBonusPercent =
            Mathf.Clamp(
                assemblyBonusPercent,
                0f,
                100f);

        Debug.Log(
    $"[CRAFT SPEED] " +
    $"Player={OwnerClientId} | " +
    $"SafeZone={(safeZone != null ? safeZone.name : "NULL")} | " +
    $"AssemblyBonus={assemblyBonusPercent}% | " +
    $"BaseTime={baseCraftTime}");

        float timeMultiplier =
            1f -
            assemblyBonusPercent / 100f;

        float finalCraftTime =
            baseCraftTime *
            timeMultiplier;

        Debug.Log(
    $"[CRAFT SPEED] FinalTime={finalCraftTime}s");

        // =====================================================
        // INSTANT CRAFT
        // =====================================================

        if (finalCraftTime <= 0f)
        {
            currentProgress.Value = 1f;
        }
        else
        {
            currentProgress.Value +=
                Time.deltaTime /
                finalCraftTime;
        }

        if (currentProgress.Value < 1f)
            return;

        Debug.Log(
            "[CRAFT 09] Crafting ukoñczony: " +
            currentModule.moduleId);

        CompleteModule(currentModule);

        moduleQueue.RemoveAt(0);
        currentModule = null;
        currentModuleId.Value = default;
        currentProgress.Value = 0f;
    }

    private void CompleteModule(ModuleDefinition module)
    {
        if (inventory == null)
        {
            Debug.LogError("[CRAFT ERROR] inventory == null");
            return;
        }

        inventory.AddModule(module.moduleId);

        ShowModuleCompletedClientRpc(
            module.moduleId);

        Debug.Log(
            "[CRAFT 10] Wywo³ano AddModule: " +
            module.moduleId);
    }

    [ClientRpc]
    private void ShowModuleCompletedClientRpc(
    string moduleId)
    {
        /*
         * Popup pokazujemy tylko
         * w³aœcicielowi tego PlayerModuleCrafting.
         */
        if (!IsOwner)
            return;

        if (ModuleDatabase.Instance == null)
            return;

        ModuleDefinition module =
            ModuleDatabase.Instance.GetModule(
                moduleId);

        if (module == null)
            return;

        NotificationManager notificationManager =
            FindFirstObjectByType<NotificationManager>();

        if (notificationManager == null)
        {
            Debug.LogWarning(
                "[NOTIFICATION] Nie znaleziono NotificationManager.");

            return;
        }

        notificationManager.ShowModuleCompleted(
            module);
    }

    private BaseCore FindOwnedCore()
    {
        BaseCore[] cores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore core in cores)
        {
            if (core == null)
                continue;

            if (!core.IsSpawned)
                continue;

            if (core.OwnerClientId ==
                OwnerClientId)
            {
                return core;
            }
        }

        return null;
    }

    private int GetTotalModuleCopies(
    string moduleId)
    {
        if (!IsServer)
            return 0;

        if (string.IsNullOrWhiteSpace(moduleId))
            return 0;

        int total = 0;

        // =====================================================
        // 1. MODULE INVENTORY
        // =====================================================

        if (inventory != null)
        {
            total +=
                inventory.GetModuleCount(
                    moduleId);
        }

        // =====================================================
        // 2. CRAFTING QUEUE
        // =====================================================

        for (int i = 0;
             i < moduleQueue.Count;
             i++)
        {
            if (moduleQueue[i].ToString() ==
                moduleId)
            {
                total++;
            }
        }

        // =====================================================
        // 3. DOCKED SHIPS
        // =====================================================

        BaseHangar[] hangars =
            FindObjectsByType<BaseHangar>(
                FindObjectsSortMode.None);

        foreach (BaseHangar hangar in hangars)
        {
            if (hangar == null)
                continue;

            if (!hangar.IsSpawned)
                continue;

            if (hangar.OwnerClientId !=
                OwnerClientId)
            {
                continue;
            }

            for (int i = 0;
                 i < hangar.dockedShips.Count;
                 i++)
            {
                DockedShipData dockedShip =
                    hangar.dockedShips[i];

                total +=
                    dockedShip.CountModule(
                        moduleId);
            }
        }

        // =====================================================
        // 4. DEPLOYED SHIPS
        // =====================================================

        ShipUnit[] ships =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit deployedShip in ships)
        {
            if (deployedShip == null)
                continue;

            if (!deployedShip.IsSpawned)
                continue;

            if (deployedShip.isDead.Value)
                continue;

            if (deployedShip.ownerId.Value !=
                OwnerClientId)
            {
                continue;
            }

            for (int slotIndex = 0;
                 slotIndex < 4;
                 slotIndex++)
            {
                if (deployedShip
                        .GetModule(slotIndex)
                        .ToString() ==
                    moduleId)
                {
                    total++;
                }
            }
        }

        return total;
    }
}