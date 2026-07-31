using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class ShipSelectionController : MonoBehaviour
{
    [SerializeField] private float shipFlightHeight = 0.5f;

    [SerializeField] private GameObject basePanel;
    [SerializeField] private CorePanelUI corePanelUI;
    [SerializeField] private CoreEnergyUI coreEnergyUI;
    [SerializeField] private HangarPanelUI hangarPanelUI;

    private PlayerBaseUnit selectedBase;

    private GameObject selectedObject;
    private ShipUnit selectedShip;
    [SerializeField] private CoreGeneratorUI[] generatorUIs;

    [Header("Ship Docking")]
    [SerializeField] private Button dockShipButton;

   
    private BaseHangar selectedShipHangar;


    private void Start()
    {
        if (dockShipButton != null)
        {
            dockShipButton.gameObject.SetActive(false);
            dockShipButton.interactable = false;
        }
    }


    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelect();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryMove();

        UpdateDockButton();
    }

    private void TrySelect()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearSelection();
            return;
        }

        // Najpierw próbujemy znaleŸæ statek.
        ShipUnit ship =
            hit.collider.GetComponentInParent<ShipUnit>();

        if (ship != null)
        {
            if (!ship.IsMine())
            {
                ClearSelection();
                return;
            }

            ClearSelection();

            selectedShip = ship;
            selectedObject = ship.gameObject;

            ship.SetSelectedLocal(true);

            selectedBase = null;

            if (basePanel != null)
                basePanel.SetActive(false);

            return;
        }

        // Je¿eli to nie statek, próbujemy zaznaczyæ bazê.
        UnitOwner owner =
            hit.collider.GetComponentInParent<UnitOwner>();

        if (owner == null || !owner.IsMine())
        {
            ClearSelection();
            return;
        }

        PlayerBaseUnit playerBase =
            owner.GetComponent<PlayerBaseUnit>();

        if (playerBase == null)
        {
            ClearSelection();
            return;
        }

        ClearSelection();

        selectedObject = owner.gameObject;
        selectedBase = playerBase;

        SelectionTarget baseSelection =
            owner.GetComponent<SelectionTarget>();

        if (baseSelection != null)
            baseSelection.SetSelected(true);

        if (basePanel != null)
            basePanel.SetActive(true);

        if (corePanelUI != null)
        {
            corePanelUI.SetCore(
                selectedBase.GetComponent<BaseCore>());
        }

        if (coreEnergyUI != null)
        {
            coreEnergyUI.SetEnergyProduction(
                selectedBase.GetComponent<BaseEnergyProduction>());
        }

        BaseEnergyGenerator[] generators =
            selectedBase.GetComponents<BaseEnergyGenerator>();

        foreach (BaseEnergyGenerator generator in generators)
        {
            int index =
                generator.GetGeneratorIndex() - 1;

            if (generatorUIs != null &&
                index >= 0 &&
                index < generatorUIs.Length)
            {
                generatorUIs[index]
                    .SetGenerator(generator);
            }
        }

        if (hangarPanelUI != null)
        {
            hangarPanelUI.SetHangar(
                selectedBase.GetComponent<BaseHangar>());
        }
    }

    private void UpdateDockButton()
    {
        if (dockShipButton == null)
            return;

        bool shipSelected =
            selectedShip != null &&
            selectedShip.IsSpawned &&
            !selectedShip.isDead.Value;

        dockShipButton.gameObject.SetActive(
            shipSelected);

        if (!shipSelected)
        {
            dockShipButton.interactable = false;
            return;
        }

        if (selectedShipHangar == null ||
            !selectedShipHangar.IsSpawned)
        {
            selectedShipHangar =
                FindOwnHangar();
        }

        dockShipButton.interactable =
            selectedShipHangar != null &&
            selectedShipHangar
                .IsShipInDockingRange(selectedShip);
    }

    private BaseHangar FindOwnHangar()
    {
        if (NetworkManager.Singleton == null)
            return null;

        ulong localClientId =
            NetworkManager.Singleton.LocalClientId;

        BaseHangar[] hangars =
            FindObjectsByType<BaseHangar>(
                FindObjectsSortMode.None);

        foreach (BaseHangar hangar in hangars)
        {
            if (hangar == null)
                continue;

            if (!hangar.IsSpawned)
                continue;

            if (hangar.OwnerClientId == localClientId)
                return hangar;
        }

        return null;
    }

    private void TryMove()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (selectedShip == null)
            return;

        if (!selectedShip.IsMine())
            return;

        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector3 target =
            hit.point;

        target.y =
            shipFlightHeight;

        selectedShip.MoveToServerRpc(target);
    }

    private void ClearSelection()
    {
        if (selectedShip != null)
            selectedShip.SetSelectedLocal(false);

        if (selectedObject != null)
        {
            SelectionTarget baseSelection =
                selectedObject.GetComponent<SelectionTarget>();

            if (baseSelection != null)
                baseSelection.SetSelected(false);
        }

        selectedShip = null;
        selectedObject = null;
        selectedBase = null;

        if (basePanel != null)
            basePanel.SetActive(false);

        if (corePanelUI != null)
            corePanelUI.SetCore(null);

        if (hangarPanelUI != null)
            hangarPanelUI.SetHangar(null);

        if (coreEnergyUI != null)
            coreEnergyUI.SetEnergyProduction(null);

        if (generatorUIs != null)
        {
            foreach (CoreGeneratorUI ui in generatorUIs)
            {
                if (ui != null)
                    ui.SetGenerator(null);
            }
        }
    }
    public void OnDockShipClicked()
    {
        if (selectedShip == null)
            return;

        if (selectedShipHangar == null)
            return;

        if (!selectedShipHangar.HasFreeDockSlot())
        {
            Debug.LogWarning(
                "[SHIP DOCK UI] Brak wolnego miejsca w hangarze.");

            return;
        }

        if (!selectedShipHangar.IsShipInDockingRange(
                selectedShip))
        {
            return;
        }

        selectedShipHangar.RequestDockShip(
            selectedShip);

        ClearSelection();
    }
}