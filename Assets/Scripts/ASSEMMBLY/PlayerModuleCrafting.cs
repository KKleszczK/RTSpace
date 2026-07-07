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
    }

    private void Update()
    {
        if (!IsServer)
            return;

        ProcessQueue();
    }

    public void RequestCraft(ModuleDefinition module)
    {
        if (module == null)
            return;

        RequestCraftServerRpc(module.moduleId);
    }

    [ServerRpc]
    private void RequestCraftServerRpc(string moduleId)
    {
        ModuleDefinition module = ModuleDatabase.Instance.GetModule(moduleId);

        if (module == null)
            return;

        if (moduleQueue.Count >= MaxQueue)
            return;

        if (!resources.CanAfford(module.metalCost, module.energyCost))
            return;

        resources.Spend(module.metalCost, module.energyCost);

        moduleQueue.Add(new FixedString64Bytes(moduleId));
    }

    public void RequestRemoveFromQueue(int index)
    {
        RemoveFromQueueServerRpc(index);
    }

    [ServerRpc]
    private void RemoveFromQueueServerRpc(int index)
    {
        if (index < 0 || index >= moduleQueue.Count)
            return;

        string id = moduleQueue[index].ToString();
        ModuleDefinition module = ModuleDatabase.Instance.GetModule(id);

        if (module != null)
        {
            resources.AddMetal(module.metalCost);
            resources.AddEnergy(module.energyCost);
        }

        moduleQueue.RemoveAt(index);

        if (index == 0)
        {
            currentModule = null;
            currentModuleId.Value = "";
            currentProgress.Value = 0f;
        }
    }

    private void ProcessQueue()
    {
        if (moduleQueue.Count == 0)
        {
            currentModule = null;
            currentModuleId.Value = "";
            currentProgress.Value = 0f;
            return;
        }

        if (currentModule == null)
        {
            string id = moduleQueue[0].ToString();
            currentModule = ModuleDatabase.Instance.GetModule(id);
            currentModuleId.Value = moduleQueue[0];
        }

        if (currentModule == null)
            return;

        currentProgress.Value += Time.deltaTime / currentModule.craftTime;

        if (currentProgress.Value >= 1f)
        {
            CompleteModule(currentModule);

            moduleQueue.RemoveAt(0);
            currentModule = null;
            currentProgress.Value = 0f;
        }
    }

    private void CompleteModule(ModuleDefinition module)
    {
        if (inventory != null)
            //inventory.AddModule(module.moduleId);

        Debug.Log("Module crafted: " + module.displayName);
    }
}