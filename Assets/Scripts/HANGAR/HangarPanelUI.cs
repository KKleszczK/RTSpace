using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HangarPanelUI : MonoBehaviour
{
    [Header("Ships")]
    [SerializeField] private List<ShipDefinition> ships = new();
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

    [Header("Docked Ships")]
    [SerializeField] private Button[] dockButtons;
    [SerializeField] private Image[] dockIcons;
    [SerializeField] private GameObject[] dockSelectors;

    [Header("Ship Class Panels")]
    [SerializeField] private ShipModulePanelUI fighterPanel;
    [SerializeField] private ShipModulePanelUI utilityPanel;
    [SerializeField] private ShipModulePanelUI minerPanel;

    [Header("Inventory")]
    [SerializeField] private ModuleInventoryPanelUI inventoryPanel;

    private BaseHangar selectedHangar;
    private int selectedDockIndex = -1;
    private BaseCore selectedCore;

    private void Start()
    {
        SetupShipButtons();
        SetupQueueButtons();
        SetupDockButtons();
        SetupShipModulePanels();

        ClearDockSelection();
        HideAllShipPanels();
    }

    private void Update()
    {
        UpdateProgress();
        UpdateQueue();
        UpdateDocked();
        RefreshSelectedShipPanel();
    }

    public void SetHangar(BaseHangar hangar)
    {
        selectedHangar = hangar;
        selectedCore = FindCoreForHangar(hangar);

        selectedDockIndex = -1;

        ClearDockSelection();
        HideAllShipPanels();

        if (inventoryPanel != null)
            inventoryPanel.Refresh();
    }

    // =========================================================
    // SETUP
    // =========================================================

    private BaseCore FindCoreForHangar(BaseHangar hangar)
    {
        if (hangar == null)
            return null;

        BaseCore core =
            hangar.GetComponent<BaseCore>();

        if (core != null)
            return core;

        core =
            hangar.GetComponentInParent<BaseCore>();

        if (core != null)
            return core;

        core =
            hangar.GetComponentInChildren<BaseCore>();

        if (core != null)
            return core;

        BaseCore[] allCores =
            FindObjectsByType<BaseCore>(
                FindObjectsSortMode.None);

        foreach (BaseCore candidate in allCores)
        {
            if (candidate.OwnerClientId ==
                hangar.OwnerClientId)
            {
                return candidate;
            }
        }

        Debug.LogWarning(
            $"[HANGAR UI] Nie znaleziono BaseCore dla hangaru owner={hangar.OwnerClientId}");

        return null;
    }

    private void SetupShipButtons()
    {
        for (int i = 0; i < shipButtons.Length; i++)
        {
            if (shipButtons[i] == null)
                continue;

            int index = i;

            shipButtons[i].onClick.AddListener(() =>
            {
                if (selectedHangar == null)
                    return;

                if (index < 0 || index >= ships.Count)
                    return;

                if (ships[index] == null)
                    return;

                selectedHangar.RequestBuildShip(ships[index]);
            });

            EventTrigger trigger =
                shipButtons[i].GetComponent<EventTrigger>();

            if (trigger == null)
            {
                trigger =
                    shipButtons[i].gameObject.AddComponent<EventTrigger>();
            }

            if (trigger.triggers == null)
            {
                trigger.triggers =
                    new List<EventTrigger.Entry>();
            }

            EventTrigger.Entry enter =
                new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerEnter
                };

            enter.callback.AddListener(_ => ShowShipCost(index));
            trigger.triggers.Add(enter);

            if (index < ships.Count &&
                index < shipButtonIcons.Length &&
                shipButtonIcons[index] != null &&
                ships[index] != null)
            {
                shipButtonIcons[index].sprite =
                    ships[index].icon;
            }
        }
    }

    private void SetupQueueButtons()
    {
        for (int i = 0; i < queueButtons.Length; i++)
        {
            if (queueButtons[i] == null)
                continue;

            int index = i;

            queueButtons[i].onClick.AddListener(() =>
            {
                if (selectedHangar != null)
                    selectedHangar.RequestRemoveFromQueue(index);
            });
        }
    }

    private void SetupDockButtons()
    {
        for (int i = 0; i < dockButtons.Length; i++)
        {
            if (dockButtons[i] == null)
                continue;

            int index = i;

            dockButtons[i].onClick.AddListener(
                () => SelectDockSlot(index));
        }
    }

    private void SetupShipModulePanels()
    {
        SetupSingleShipModulePanel(fighterPanel);
        SetupSingleShipModulePanel(utilityPanel);
        SetupSingleShipModulePanel(minerPanel);
    }

    private void SetupSingleShipModulePanel(
        ShipModulePanelUI panel)
    {
        if (panel == null)
            return;

        ModuleSlotUI[] slots = panel.GetSlots();

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Setup(this, i);
        }
    }

    // =========================================================
    // MODULE INSTALL / REMOVE
    // =========================================================

    public void RequestInstallModule(
        int slotIndex,
        ModuleDefinition module)
    {
        if (selectedHangar == null)
            return;

        if (selectedDockIndex < 0 ||
            selectedDockIndex >= selectedHangar.dockedShips.Count)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex > 3)
            return;

        if (module == null)
            return;

        if (string.IsNullOrWhiteSpace(module.moduleId))
            return;

        selectedHangar.RequestInstallModule(
            selectedDockIndex,
            slotIndex,
            module.moduleId);
    }

    public void RequestRemoveModule(int slotIndex)
    {
        if (selectedHangar == null)
            return;

        if (selectedDockIndex < 0 ||
            selectedDockIndex >= selectedHangar.dockedShips.Count)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex > 3)
            return;

        selectedHangar.RequestRemoveModule(
            selectedDockIndex,
            slotIndex);
    }

    // =========================================================
    // SHIP CLASS PANELS
    // =========================================================

    private void HideAllShipPanels()
    {
        if (fighterPanel != null)
            fighterPanel.SetVisible(false);

        if (utilityPanel != null)
            utilityPanel.SetVisible(false);

        if (minerPanel != null)
            minerPanel.SetVisible(false);
    }

    private void RefreshSelectedShipPanel()
    {
        if (selectedHangar == null)
        {
            HideAllShipPanels();
            return;
        }

        if (selectedDockIndex < 0 ||
            selectedDockIndex >= selectedHangar.dockedShips.Count)
        {
            HideAllShipPanels();
            return;
        }

        DockedShipData shipData =
            selectedHangar.dockedShips[selectedDockIndex];

        if (ShipDatabase.Instance == null)
        {
            HideAllShipPanels();
            return;
        }

        ShipDefinition shipDefinition =
            ShipDatabase.Instance.GetShip(
                shipData.shipId.ToString());

        if (shipDefinition == null)
        {
            HideAllShipPanels();
            return;
        }

        ShipModulePanelUI selectedPanel =
            GetPanelForShipType(shipDefinition.shipType);

        HideAllShipPanels();

        if (selectedPanel == null)
            return;

        selectedPanel.SetVisible(true);
        int coreTier =
    selectedCore != null
        ? selectedCore.tier.Value
        : 1;

        selectedPanel.Refresh(
            shipDefinition,
            shipData,
            coreTier);
    }

    private ShipModulePanelUI GetPanelForShipType(
        ShipType shipType)
    {
        switch (shipType)
        {
            case ShipType.Fighter:
                return fighterPanel;

            case ShipType.Utility:
                return utilityPanel;

            case ShipType.Miner:
                return minerPanel;

            default:
                return null;
        }
    }

    // =========================================================
    // SHIP BUILD UI
    // =========================================================

    private void ShowShipCost(int index)
    {
        if (index < 0 || index >= ships.Count)
            return;

        ShipDefinition ship = ships[index];

        if (ship == null)
            return;

        if (metalText != null)
            metalText.text = "M: " + ship.metalCost;

        if (energyText != null)
            energyText.text = "E: " + ship.energyCost;

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
        for (int i = 0; i < queueIcons.Length; i++)
        {
            if (queueIcons[i] == null)
                continue;

            bool hasShip =
                selectedHangar != null &&
                i < selectedHangar.buildQueue.Count;

            if (hasShip)
            {
                string shipId =
                    selectedHangar.buildQueue[i].ToString();

                ShipDefinition ship =
                    ShipDatabase.Instance != null
                        ? ShipDatabase.Instance.GetShip(shipId)
                        : null;

                queueIcons[i].sprite =
                    ship != null
                        ? ship.icon
                        : emptySprite;

                if (i < queueButtons.Length &&
                    queueButtons[i] != null)
                {
                    queueButtons[i].interactable = true;
                }
            }
            else
            {
                queueIcons[i].sprite = emptySprite;

                if (i < queueButtons.Length &&
                    queueButtons[i] != null)
                {
                    queueButtons[i].interactable = false;
                }
            }
        }
    }

    // =========================================================
    // DOCKED SHIPS
    // =========================================================

    private void UpdateDocked()
    {
        for (int i = 0; i < dockIcons.Length; i++)
        {
            if (dockIcons[i] == null)
                continue;

            bool hasShip =
                selectedHangar != null &&
                i < selectedHangar.dockedShips.Count;

            if (hasShip)
            {
                DockedShipData shipData =
                    selectedHangar.dockedShips[i];

                ShipDefinition ship =
                    ShipDatabase.Instance != null
                        ? ShipDatabase.Instance.GetShip(
                            shipData.shipId.ToString())
                        : null;

                dockIcons[i].sprite =
                    ship != null
                        ? ship.icon
                        : emptySprite;

                if (i < dockButtons.Length &&
                    dockButtons[i] != null)
                {
                    dockButtons[i].interactable = true;
                }
            }
            else
            {
                dockIcons[i].sprite = emptySprite;

                if (i < dockButtons.Length &&
                    dockButtons[i] != null)
                {
                    dockButtons[i].interactable = false;
                }

                if (selectedDockIndex == i)
                {
                    selectedDockIndex = -1;
                    HideAllShipPanels();
                }
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
        RefreshSelectedShipPanel();
    }

    private void RefreshDockSelectors()
    {
        for (int i = 0; i < dockSelectors.Length; i++)
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