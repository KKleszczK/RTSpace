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

        foreach (FixedString64Bytes moduleId in inventory.modules)
        {
            ModuleDefinition module =
                ModuleDatabase.Instance != null
                    ? ModuleDatabase.Instance.GetModule(
                        moduleId.ToString())
                    : null;

            if (module == null)
                continue;

            DraggableModuleUI item =
                Instantiate(
                    modulePrefab,
                    content);

            item.Setup(module);
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
}