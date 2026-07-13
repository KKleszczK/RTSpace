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

    private void Awake()
    {
        resources = GetComponent<PlayerResources>();
        inventory = GetComponent<PlayerModuleInventory>();

        moduleQueue = new NetworkList<FixedString64Bytes>();

        if (resources == null)
            Debug.LogError("[CRAFT ERROR] Brak PlayerResources na PlayerPrefab", gameObject);

        if (inventory == null)
            Debug.LogError("[CRAFT ERROR] Brak PlayerModuleInventory na PlayerPrefab", gameObject);
    }

    private void Update()
    {
        if (!IsServer)
            return;

        ProcessQueue();
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

        float craftTime =
            Mathf.Max(0.01f, currentModule.craftTime);

        currentProgress.Value +=
            Time.deltaTime / craftTime;

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

        Debug.Log(
            "[CRAFT 10] Wywo³ano AddModule: " +
            module.moduleId);
    }
}