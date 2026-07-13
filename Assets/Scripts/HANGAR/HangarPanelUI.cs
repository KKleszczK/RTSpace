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

    [Header("Docked")]
    [SerializeField] private Button[] dockButtons;
    [SerializeField] private Image[] dockIcons;
    [SerializeField] private GameObject[] dockSelectors;

    private BaseHangar selectedHangar;
    private int selectedDockIndex = -1;

    private void Start()
    {
        for (int i = 0; i < shipButtons.Length; i++)
        {
            int index = i;

            shipButtons[i].onClick.AddListener(() =>
            {
                if (selectedHangar != null && index < ships.Count)
                    selectedHangar.RequestBuildShip(ships[index]);
            });

            EventTrigger trigger = shipButtons[i].gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };

            enter.callback.AddListener((_) => ShowShipCost(index));
            trigger.triggers.Add(enter);

            if (index < ships.Count && index < shipButtonIcons.Length)
                shipButtonIcons[index].sprite = ships[index].icon;
        }

        for (int i = 0; i < queueButtons.Length; i++)
        {
            int index = i;
            queueButtons[i].onClick.AddListener(() =>
            {
                if (selectedHangar != null)
                    selectedHangar.RequestRemoveFromQueue(index);
            });
        }

        for (int i = 0; i < dockButtons.Length; i++)
        {
            int index = i;
            dockButtons[i].onClick.AddListener(() => SelectDockSlot(index));
        }

        ClearDockSelection();
    }

    private void Update()
    {
        UpdateProgress();
        UpdateQueue();
        UpdateDocked();
    }

    public void SetHangar(BaseHangar hangar)
    {
        selectedHangar = hangar;
        selectedDockIndex = -1;
        ClearDockSelection();
    }

    private void ShowShipCost(int index)
    {
        if (index < 0 || index >= ships.Count)
            return;

        ShipDefinition ship = ships[index];

        metalText.text = "M: " + ship.metalCost;
        energyText.text = "E: " + ship.energyCost;
        timeText.text = "T: " + Mathf.RoundToInt(ship.buildTime) + "s";
    }

    private void UpdateProgress()
    {
        if (progressBar == null)
            return;

        float progress = selectedHangar != null
            ? selectedHangar.buildProgress.Value
            : 0f;

        Vector2 size = progressBar.sizeDelta;
        size.x = maxProgressWidth * progress;
        progressBar.sizeDelta = size;
    }

    private void UpdateQueue()
    {
        for (int i = 0; i < queueIcons.Length; i++)
        {
            if (selectedHangar != null && i < selectedHangar.buildQueue.Count)
            {
                string id = selectedHangar.buildQueue[i].ToString();
                ShipDefinition ship = ShipDatabase.Instance.GetShip(id);

                queueIcons[i].sprite = ship != null ? ship.icon : emptySprite;
                queueButtons[i].interactable = true;
            }
            else
            {
                queueIcons[i].sprite = emptySprite;
                queueButtons[i].interactable = false;
            }
        }
    }

    private void UpdateDocked()
    {
        for (int i = 0; i < dockIcons.Length; i++)
        {
            if (selectedHangar != null && i < selectedHangar.dockedShips.Count)
            {
                string id = selectedHangar.dockedShips[i].ToString();
                ShipDefinition ship = ShipDatabase.Instance.GetShip(id);

                dockIcons[i].sprite = ship != null ? ship.icon : emptySprite;
                dockButtons[i].interactable = true;
            }
            else
            {
                dockIcons[i].sprite = emptySprite;
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

        if (index < 0 || index >= selectedHangar.dockedShips.Count)
            return;

        selectedDockIndex = index;
        RefreshDockSelectors();
    }

    private void RefreshDockSelectors()
    {
        for (int i = 0; i < dockSelectors.Length; i++)
        {
            if (dockSelectors[i] != null)
                dockSelectors[i].SetActive(i == selectedDockIndex);
        }
    }

    private void ClearDockSelection()
    {
        selectedDockIndex = -1;
        RefreshDockSelectors();
    }
}