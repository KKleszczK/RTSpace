using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HangarPanelUI : MonoBehaviour
{
    [Header("Ships")]
    [SerializeField]
    private List<ShipDefinition> ships = new();

    [SerializeField] private Button[] shipButtons;
    [SerializeField] private Image[] shipButtonIcons;

    [Header("Info")]
    [SerializeField] private TMP_Text metalText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text timeText;

    [Header("Progress")]
    [SerializeField] private RectTransform progressBar;
    [SerializeField] private float maxProgressWidth = 500f;

    [Header("Queue")]
    [SerializeField] private Button[] queueButtons;
    [SerializeField] private Image[] queueIcons;
    [SerializeField] private Sprite emptySprite;

    [Header("Docked")]
    [SerializeField] private Button[] dockButtons;
    [SerializeField] private Image[] dockIcons;
    [SerializeField] private GameObject[] dockSelectors;

    [Header("Selected Ship")]
    [SerializeField]
    private GameObject selectedShipPanel;

    [SerializeField]
    private Image selectedShipIcon;

    [SerializeField]
    private TMP_Text selectedShipName;

    [Header("Module Slots")]
    [SerializeField]
    private ModuleSlotUI[] moduleSlots;

    [Header("Inventory")]
    [SerializeField]
    private ModuleInventoryPanelUI inventoryPanel;

    private BaseHangar selectedHangar;
    private int selectedDockIndex = -1;

    private void Start()
    {
        SetupShipButtons();
        SetupQueueButtons();
        SetupDockButtons();
        SetupModuleSlots();

        ClearDockSelection();
        RefreshSelectedShip();
    }

    private void Update()
    {
        UpdateProgress();
        UpdateQueue();
        UpdateDocked();
        RefreshSelectedShip();
    }

    public void SetHangar(BaseHangar hangar)
    {
        selectedHangar = hangar;
        selectedDockIndex = -1;

        ClearDockSelection();
        RefreshSelectedShip();

        if (inventoryPanel != null)
            inventoryPanel.Refresh();
    }

    // =========================================================
    // SETUP
    // =========================================================

    private void SetupShipButtons()
    {
        for (int i = 0;
             i < shipButtons.Length;
             i++)
        {
            int index = i;

            shipButtons[i].onClick.AddListener(
                () =>
                {
                    if (selectedHangar != null &&
                        index < ships.Count)
                    {
                        selectedHangar.RequestBuildShip(
                            ships[index]);
                    }
                });

            EventTrigger trigger =
                shipButtons[i]
                    .GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger =
                    shipButtons[i]
                        .gameObject
                        .AddComponent<EventTrigger>();
            }

            EventTrigger.Entry enter =
                new EventTrigger.Entry
                {
                    eventID =
                        EventTriggerType.PointerEnter
                };

            enter.callback.AddListener(
                _ => ShowShipCost(index));

            trigger.triggers.Add(enter);

            if (index < ships.Count &&
                index < shipButtonIcons.Length)
            {
                shipButtonIcons[index].sprite =
                    ships[index].icon;
            }
        }
    }

    private void SetupQueueButtons()
    {
        for (int i = 0;
             i < queueButtons.Length;
             i++)
        {
            int index = i;

            queueButtons[i].onClick.AddListener(
                () =>
                {
                    if (selectedHangar != null)
                    {
                        selectedHangar
                            .RequestRemoveFromQueue(index);
                    }
                });
        }
    }

    private void SetupDockButtons()
    {
        for (int i = 0;
             i < dockButtons.Length;
             i++)
        {
            int index = i;

            dockButtons[i].onClick.AddListener(
                () => SelectDockSlot(index));
        }
    }

    private void SetupModuleSlots()
    {
        for (int i = 0;
             i < moduleSlots.Length;
             i++)
        {
            if (moduleSlots[i] != null)
                moduleSlots[i].Setup(this, i);
        }
    }

    // =========================================================
    // MODULE REQUESTS
    // =========================================================

    public void RequestInstallModule(
        int slotIndex,
        ModuleDefinition module)
    {
        if (selectedHangar == null)
            return;

        if (selectedDockIndex < 0)
            return;

        if (module == null)
            return;

        selectedHangar.RequestInstallModule(
            selectedDockIndex,
            slotIndex,
            module.moduleId);
    }

    public void RequestRemoveModule(
        int slotIndex)
    {
        if (selectedHangar == null)
            return;

        if (selectedDockIndex < 0)
            return;

        selectedHangar.RequestRemoveModule(
            selectedDockIndex,
            slotIndex);
    }

    // =========================================================
    // UI
    // =========================================================

    private void ShowShipCost(int index)
    {
        if (index < 0 ||
            index >= ships.Count)
        {
            return;
        }

        ShipDefinition ship = ships[index];

        if (metalText != null)
            metalText.text =
                "M: " + ship.metalCost;

        if (energyText != null)
            energyText.text =
                "E: " + ship.energyCost;

        if (timeText != null)
        {
            timeText.text =
                "T: " +
                Mathf.RoundToInt(ship.buildTime) +
                "s";
        }
    }

    private void UpdateProgress()
    {
        if (progressBar == null)
            return;

        float progress =
            selectedHangar != null
                ? selectedHangar.buildProgress.Value
                : 0f;

        Vector2 size =
            progressBar.sizeDelta;

        size.x =
            maxProgressWidth *
            Mathf.Clamp01(progress);

        progressBar.sizeDelta = size;
    }

    private void UpdateQueue()
    {
        for (int i = 0;
             i < queueIcons.Length;
             i++)
        {
            bool hasShip =
                selectedHangar != null &&
                i < selectedHangar.buildQueue.Count;

            if (hasShip)
            {
                string shipId =
                    selectedHangar
                        .buildQueue[i]
                        .ToString();

                ShipDefinition ship =
                    ShipDatabase.Instance
                        .GetShip(shipId);

                queueIcons[i].sprite =
                    ship != null
                        ? ship.icon
                        : emptySprite;

                queueButtons[i].interactable = true;
            }
            else
            {
                queueIcons[i].sprite =
                    emptySprite;

                queueButtons[i].interactable = false;
            }
        }
    }

    private void UpdateDocked()
    {
        for (int i = 0;
             i < dockIcons.Length;
             i++)
        {
            bool hasShip =
                selectedHangar != null &&
                i < selectedHangar.dockedShips.Count;

            if (hasShip)
            {
                DockedShipData shipData =
                    selectedHangar.dockedShips[i];

                ShipDefinition ship =
                    ShipDatabase.Instance.GetShip(
                        shipData.shipId.ToString());

                dockIcons[i].sprite =
                    ship != null
                        ? ship.icon
                        : emptySprite;

                dockButtons[i].interactable = true;
            }
            else
            {
                dockIcons[i].sprite =
                    emptySprite;

                dockButtons[i].interactable = false;

                if (selectedDockIndex == i)
                    selectedDockIndex = -1;
            }
        }

        RefreshDockSelectors();
    }

    private void SelectDockSlot(int index)
    {
        if (selectedHangar == null)
            return;

        if (index < 0 ||
            index >= selectedHangar.dockedShips.Count)
        {
            return;
        }

        selectedDockIndex = index;

        RefreshDockSelectors();
        RefreshSelectedShip();
    }

    private void RefreshSelectedShip()
    {
        bool hasSelection =
            selectedHangar != null &&
            selectedDockIndex >= 0 &&
            selectedDockIndex <
            selectedHangar.dockedShips.Count;

        if (selectedShipPanel != null)
            selectedShipPanel.SetActive(hasSelection);

        if (!hasSelection)
        {
            ClearModuleSlots();
            return;
        }

        DockedShipData shipData =
            selectedHangar
                .dockedShips[selectedDockIndex];

        ShipDefinition ship =
            ShipDatabase.Instance.GetShip(
                shipData.shipId.ToString());

        if (selectedShipIcon != null)
        {
            selectedShipIcon.sprite =
                ship != null
                    ? ship.icon
                    : emptySprite;
        }

        if (selectedShipName != null)
        {
            selectedShipName.text =
                ship != null
                    ? ship.displayName
                    : "UNKNOWN SHIP";
        }

        RefreshModuleSlots(shipData);
    }

    private void RefreshModuleSlots(
        DockedShipData shipData)
    {
        for (int i = 0;
             i < moduleSlots.Length;
             i++)
        {
            if (moduleSlots[i] == null)
                continue;

            string moduleId =
                shipData.GetModule(i).ToString();

            if (string.IsNullOrEmpty(moduleId))
            {
                moduleSlots[i].Clear();
                continue;
            }

            ModuleDefinition module =
                ModuleDatabase.Instance.GetModule(
                    moduleId);

            moduleSlots[i].SetModule(module);
        }
    }

    private void ClearModuleSlots()
    {
        foreach (ModuleSlotUI slot
                 in moduleSlots)
        {
            if (slot != null)
                slot.Clear();
        }
    }

    private void RefreshDockSelectors()
    {
        for (int i = 0;
             i < dockSelectors.Length;
             i++)
        {
            if (dockSelectors[i] != null)
            {
                dockSelectors[i].SetActive(
                    i == selectedDockIndex);
            }
        }
    }

    private void ClearDockSelection()
    {
        selectedDockIndex = -1;
        RefreshDockSelectors();
    }
}