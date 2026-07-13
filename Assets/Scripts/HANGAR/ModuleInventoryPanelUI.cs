using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ModuleInventoryPanelUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private DraggableModuleUI modulePrefab;

    private PlayerModuleInventory inventory;

    private void Update()
    {
        if (inventory == null)
        {
            FindLocalInventory();
        }
    }

    private void FindLocalInventory()
    {
        PlayerModuleInventory[] inventories =
            FindObjectsByType<PlayerModuleInventory>(
                FindObjectsSortMode.None);

        foreach (PlayerModuleInventory candidate in inventories)
        {
            if (candidate.IsOwner)
            {
                inventory = candidate;

                inventory.modules.OnListChanged += OnInventoryChanged;

                RefreshInventory();
                return;
            }
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.modules.OnListChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        Debug.Log("5. Inventory UI dosta³o zmianê");

        RefreshInventory();
    }

    private void RefreshInventory()
    {
        Debug.Log("6. RefreshInventory()");
        if (content == null || modulePrefab == null || inventory == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        foreach (FixedString64Bytes moduleId in inventory.modules)
        {
            ModuleDefinition module =
                ModuleDatabase.Instance.GetModule(moduleId.ToString());

            if (module == null)
                continue;

            DraggableModuleUI item =
                Instantiate(modulePrefab, content);

            item.Setup(module);
        }
    }
    public void Refresh()
    {
        RefreshInventory();
    }
}