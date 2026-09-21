using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class ModuleInventoryPanelUI :
    MonoBehaviour,
    IDropHandler
{
    [SerializeField] private Transform content;
    [SerializeField] private DraggableModuleUI modulePrefab;

    [SerializeField] private HangarPanelUI hangarPanel;

    private PlayerModuleInventory inventory;

    private void Update()
    {
        if (inventory == null)
            FindLocalInventory();
    }

    private void FindLocalInventory()
    {
        PlayerModuleInventory[] inventories =
            FindObjectsByType<PlayerModuleInventory>(
                FindObjectsSortMode.None);

        foreach (PlayerModuleInventory candidate in inventories)
        {
            if (!candidate.IsOwner)
                continue;

            inventory = candidate;

            inventory.modules.OnListChanged +=
                OnInventoryChanged;

            RefreshInventory();
            return;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.modules.OnListChanged -=
                OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(
        NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        Debug.Log(
            "[MODULE INVENTORY UI] Inventory zosta³o zmienione.");

        RefreshInventory();
    }

    private void RefreshInventory()
    {
        if (content == null ||
            modulePrefab == null ||
            inventory == null)
        {
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(
                content.GetChild(i).gameObject);
        }

        Dictionary<string, int> moduleCounts =
            new Dictionary<string, int>();

        foreach (FixedString64Bytes moduleId in inventory.modules)
        {
            string id = moduleId.ToString();

            if (moduleCounts.ContainsKey(id))
                moduleCounts[id]++;
            else
                moduleCounts[id] = 1;
        }

        List<ModuleDefinition> sortedModules =
            new List<ModuleDefinition>();

        foreach (var pair in moduleCounts)
        {
            ModuleDefinition module =
                ModuleDatabase.Instance != null
                    ? ModuleDatabase.Instance.GetModule(pair.Key)
                    : null;

            if (module != null)
                sortedModules.Add(module);
        }

        sortedModules.Sort(
            (a, b) =>
            {
                // T3 -> T2 -> T1
                int tierComparison =
                    ((int)b.tier).CompareTo((int)a.tier);

                if (tierComparison != 0)
                    return tierComparison;

                return string.Compare(
                    a.displayName,
                    b.displayName,
                    System.StringComparison.OrdinalIgnoreCase);
            });

        foreach (ModuleDefinition module in sortedModules)
        {
            DraggableModuleUI item =
                Instantiate(
                    modulePrefab,
                    content);

            item.Setup(
                module,
                moduleCounts[module.moduleId],
                this);
        }
    }

    // =========================================================
    // DROP MODU£U ZE STATKU
    // =========================================================

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        ModuleSlotUI sourceSlot =
            eventData.pointerDrag.GetComponent<ModuleSlotUI>();

        if (sourceSlot == null)
            return;

        if (!sourceSlot.IsDragging)
            return;

        ModuleDefinition module =
            sourceSlot.GetModule();

        if (module == null)
            return;

        Debug.Log(
            $"[MODULE INVENTORY DROP] Zdejmowanie module={module.moduleId} ze slotu={sourceSlot.SlotIndex}");

        sourceSlot.RequestRemoveFromShip();
    }

    public void Refresh()
    {
        RefreshInventory();
    }


    public void RequestAutoInstall(
    ModuleDefinition module)
    {
        if (module == null ||
            hangarPanel == null)
        {
            return;
        }

        hangarPanel.RequestAutoInstallModule(
            module);
    }


}
