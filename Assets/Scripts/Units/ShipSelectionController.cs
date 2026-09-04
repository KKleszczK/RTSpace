using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShipSelectionController : MonoBehaviour
{
    [SerializeField] private float shipFlightHeight = 0.5f;

    [SerializeField] private GameObject basePanel;
    [SerializeField] private CorePanelUI corePanelUI;
    [SerializeField] private CoreEnergyUI coreEnergyUI;
    [SerializeField] private HangarPanelUI hangarPanelUI;

    private PlayerBaseUnit selectedBase;

    private GameObject selectedObject;
    private readonly List<ShipUnit> selectedShips = new();
    private readonly List<ShipUnit> boxPreviewShips = new();

    private ShipUnit selectedShip;//tmp
    [SerializeField] private CoreGeneratorUI[] generatorUIs;

    [Header("Ship Docking")]
    [SerializeField] private Button dockShipButton;

   
    private BaseHangar selectedShipHangar;

    [Header("Box Selection")]
    [SerializeField]
    private RectTransform selectionBox;

    [SerializeField, Min(1f)]
    private float boxSelectionThreshold = 10f;

    private Vector2 boxStartPosition;
    private bool isBoxSelecting;

    [Header("Type Selection")]
    [SerializeField]
    private float doubleClickTime = 0.3f;

    private float lastShipClickTime = -10f;
    private ShipUnit lastClickedShip;

    [Header("Move Command")]
    [SerializeField]
    private MoveCommandMarker moveCommandMarkerPrefab;

    [SerializeField]
    private float moveCommandMarkerHeight = 0.05f;


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
            StartSelection();

        if (Mouse.current.leftButton.isPressed)
            UpdateSelectionBox();

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            FinishSelection();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryMove();

        TryStop();

        UpdateDockButton();

        RefreshAttackTargetMarkers();
    }

    private void TrySelect()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Camera.main == null)
            return;

        bool shiftPressed =
            IsShiftPressed();

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue());

        // =========================================================
        // NIC NIE TRAFILIŒMY
        // =========================================================

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            // Shift + puste miejsce
            // nie zmienia aktualnego zaznaczenia.
            if (!shiftPressed)
                ClearSelection();

            return;
        }

        // =========================================================
        // STATEK
        // =========================================================

        ShipUnit ship =
            hit.collider.GetComponentInParent<ShipUnit>();

        if (ship != null)
        {
            // =========================================================
            // OBCY STATEK
            // =========================================================

            if (!ship.IsMine())
            {
                if (!shiftPressed)
                    ClearSelection();

                return;
            }

            // =========================================================
            // DOUBLE CLICK
            // =========================================================

            bool doubleClick =
                lastClickedShip == ship &&
                Time.unscaledTime -
                lastShipClickTime <=
                doubleClickTime;

            // Zapamiêtujemy klikniêcie.
            lastClickedShip =
                ship;

            lastShipClickTime =
                Time.unscaledTime;

            // =========================================================
            // ALT CLICK / DOUBLE CLICK
            // =========================================================

            if (IsAltPressed() ||
                doubleClick)
            {
                SelectShipsOfSameTypeOnScreen(
                    ship,
                    shiftPressed);

                // Resetujemy double click,
                // ¿eby trzeci szybki klik nie zosta³
                // potraktowany jako kolejny double click.
                lastClickedShip = null;
                lastShipClickTime = -10f;

                return;
            }

            // =========================================================
            // NORMAL CLICK
            // =========================================================

            if (shiftPressed)
            {
                ToggleShipSelection(ship);
            }
            else
            {
                SelectSingleShip(ship);
            }

            selectedBase = null;
            selectedObject = null;

            if (basePanel != null)
                basePanel.SetActive(false);

            return;
        }

        // =========================================================
        // BAZA
        // =========================================================

        UnitOwner owner =
            hit.collider.GetComponentInParent<UnitOwner>();

        // Trafiliœmy np. w pod³o¿e, asteroidê
        // albo obcy obiekt.
        if (owner == null ||
            !owner.IsMine())
        {
            if (!shiftPressed)
                ClearSelection();

            return;
        }

        PlayerBaseUnit playerBase =
            owner.GetComponent<PlayerBaseUnit>();

        if (playerBase == null)
        {
            if (!shiftPressed)
                ClearSelection();

            return;
        }

        // =========================================================
        // W£ASNA BAZA
        // =========================================================

        // Na razie baza pozostaje osobnym typem zaznaczenia.
        // Klikniêcie bazy czyœci zaznaczone statki.
        ClearSelection();

        selectedObject =
            owner.gameObject;

        selectedBase =
            playerBase;

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

        if (selectedShips.Count == 0)
            return;

        if (Camera.main == null)
            return;

        Ray ray =
            Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue());

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return;
        }

        bool queueCommand =
            GameInputManager.Instance != null &&
            GameInputManager.Instance.QueueCommandPressed;

        // =========================================================
        // RIGHT CLICK ON ENEMY SHIP = ATTACK
        // =========================================================

        ShipUnit targetShip =
            hit.collider.GetComponentInParent<ShipUnit>();

        if (targetShip != null &&
            targetShip.IsSpawned &&
            !targetShip.isDead.Value &&
            !targetShip.IsMine())
        {
            foreach (ShipUnit ship in selectedShips)
            {
                if (ship == null)
                    continue;

                if (!ship.IsMine())
                    continue;

                if (!ship.IsSpawned)
                    continue;

                if (ship.isDead.Value)
                    continue;

                if (queueCommand)
                {
                    ship.QueueVisualAttackCommand(
                        targetShip);
                }
                else
                {
                    ship.SetVisualAttackCommand(
                        targetShip);
                }

                ship.AttackServerRpc(
                    new NetworkObjectReference(
                        targetShip.NetworkObject),
                    queueCommand);
            }

            return;
        }

        Vector3 target =
            hit.point;

        ShowMoveCommandMarker(
            hit.point);

        target.y =
            shipFlightHeight;

        // =========================================================
        // SINGLE SHIP
        // =========================================================

        if (selectedShips.Count == 1)
        {
            ShipUnit ship =
                selectedShips[0];

            if (ship == null ||
                !ship.IsMine() ||
                !ship.IsSpawned ||
                ship.isDead.Value)
            {
                return;
            }

            ship.MoveToServerRpc(
                target,
                queueCommand);

            if (queueCommand)
            {
                ship.QueueVisualMoveCommand(
                    target);
            }
            else
            {
                ship.SetVisualMoveCommand(
                    target);
            }

            return;
        }

        // =========================================================
        // GROUP CENTER
        // =========================================================

        Vector3 groupCenter =
            Vector3.zero;

        int validShipCount = 0;

        foreach (ShipUnit ship in selectedShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsMine())
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            groupCenter +=
                ship.transform.position;

            validShipCount++;
        }

        if (validShipCount == 0)
            return;

        groupCenter /=
            validShipCount;

        // =========================================================
        // INDIVIDUAL TARGETS
        // =========================================================

        foreach (ShipUnit ship in selectedShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsMine())
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            Vector3 offset =
                ship.transform.position -
                groupCenter;

            offset.y = 0f;

            Vector3 shipTarget =
                target + offset;

            shipTarget.y =
                shipFlightHeight;

            ship.MoveToServerRpc(
                shipTarget,
                queueCommand);

            if (queueCommand)
            {
                ship.QueueVisualMoveCommand(
                    shipTarget);
            }
            else
            {
                ship.SetVisualMoveCommand(
                    shipTarget);
            }
        }
    }

    private void ClearSelection()
    {
        for (int i = 0;
            i < selectedShips.Count;
            i++)
            {
                ShipUnit ship =
                    selectedShips[i];

                if (ship != null)
                    ship.SetSelectedLocal(false);
            }

        selectedShips.Clear();

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

    ///////////////////////////////////////////////
    ///     NEW SELECTION SYSTEM 
    ///////////////////////////////////////////////
    

    private void SelectSingleShip(
    ShipUnit ship)
    {
        ClearSelection();

        if (ship == null)
            return;

        selectedShips.Add(ship);

        ship.SetSelectedLocal(true);

        UpdatePrimarySelectedShip();
    }

    private void ToggleShipSelection(
    ShipUnit ship)
    {
        if (ship == null)
            return;

        if (selectedShips.Contains(ship))
        {
            selectedShips.Remove(ship);

            ship.SetSelectedLocal(false);
        }
        else
        {
            selectedShips.Add(ship);

            ship.SetSelectedLocal(true);
        }

        UpdatePrimarySelectedShip();
    }

    private void UpdatePrimarySelectedShip()
    {
        if (selectedShips.Count == 1)
        {
            selectedShip =
                selectedShips[0];
        }
        else
        {
            selectedShip = null;
        }
    }

    private bool IsShiftPressed()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed);
    }

    private void StartSelection()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        boxStartPosition =
            Mouse.current.position.ReadValue();

        isBoxSelecting = true;

        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
    }

    private void UpdateSelectionBox()
    {
        if (!isBoxSelecting)
            return;

        if (selectionBox == null)
            return;

        Vector2 currentPosition =
            Mouse.current.position.ReadValue();

        Vector2 difference =
            currentPosition -
            boxStartPosition;

        if (difference.magnitude <
            boxSelectionThreshold)
        {
            selectionBox.gameObject.SetActive(false);
            ClearBoxPreview();
            return;
        }

        selectionBox.gameObject.SetActive(true);

        Vector2 center =
            (boxStartPosition +
             currentPosition) * 0.5f;

        Vector2 size =
            new Vector2(
                Mathf.Abs(difference.x),
                Mathf.Abs(difference.y));

        selectionBox.position =
            center;

        selectionBox.sizeDelta =
            size;

        UpdateBoxPreview(
            boxStartPosition,
            currentPosition);
    }

    private void FinishSelection()
    {
        if (!isBoxSelecting)
            return;

        isBoxSelecting = false;

        Vector2 endPosition =
            Mouse.current.position.ReadValue();

        Vector2 difference =
            endPosition -
            boxStartPosition;

        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }

        // To by³o zwyk³e klikniêcie.
        if (difference.magnitude <
            boxSelectionThreshold)
        {
            ClearBoxPreview();

            TrySelect();
            return;
        }

        SelectShipsInBox(
            boxStartPosition,
            endPosition);
        ClearBoxPreview();
    }

    private void SelectShipsInBox(
    Vector2 start,
    Vector2 end)
    {
        if (Camera.main == null)
            return;

        float minX =
            Mathf.Min(start.x, end.x);

        float maxX =
            Mathf.Max(start.x, end.x);

        float minY =
            Mathf.Min(start.y, end.y);

        float maxY =
            Mathf.Max(start.y, end.y);

        Rect selectionRect =
            Rect.MinMaxRect(
                minX,
                minY,
                maxX,
                maxY);

        bool shiftPressed =
            IsShiftPressed();

        // Bez Shift nowe zaznaczenie
        // zastêpuje poprzednie.
        if (!shiftPressed)
            ClearSelection();

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            if (!ship.IsMine())
                continue;

            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(
                    ship.transform.position);

            // Statek znajduje siê za kamer¹.
            if (screenPosition.z <= 0f)
                continue;

            Vector2 shipScreenPosition =
                new Vector2(
                    screenPosition.x,
                    screenPosition.y);

            if (!selectionRect.Contains(
                    shipScreenPosition))
            {
                continue;
            }

            // Przy boxie DODAJEMY.
            // Nie u¿ywamy Toggle, poniewa¿ statek,
            // który by³ ju¿ zaznaczony, ma pozostaæ
            // zaznaczony.
            if (!selectedShips.Contains(ship))
            {
                selectedShips.Add(ship);

                ship.SetSelectedLocal(true);
            }
        }

        UpdatePrimarySelectedShip();
    }

    private void ClearBoxPreview()
    {
        foreach (ShipUnit ship in boxPreviewShips)
        {
            if (ship != null)
            {
                ship.SetBoxSelectionPreviewLocal(
                    false);
            }
        }

        boxPreviewShips.Clear();
    }

    private void UpdateBoxPreview(
    Vector2 start,
    Vector2 end)
    {
        if (Camera.main == null)
            return;

        // Najpierw wy³¹czamy preview
        // z poprzedniej pozycji boxa.
        ClearBoxPreview();

        float minX =
            Mathf.Min(start.x, end.x);

        float maxX =
            Mathf.Max(start.x, end.x);

        float minY =
            Mathf.Min(start.y, end.y);

        float maxY =
            Mathf.Max(start.y, end.y);

        Rect selectionRect =
            Rect.MinMaxRect(
                minX,
                minY,
                maxX,
                maxY);

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            if (!ship.IsMine())
                continue;

            // Statek ju¿ zaznaczony nie potrzebuje
            // dodatkowego markera preview.
            if (selectedShips.Contains(ship))
                continue;

            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(
                    ship.transform.position);

            // Za kamer¹.
            if (screenPosition.z <= 0f)
                continue;

            Vector2 shipScreenPosition =
                new Vector2(
                    screenPosition.x,
                    screenPosition.y);

            if (!selectionRect.Contains(
                    shipScreenPosition))
            {
                continue;
            }

            boxPreviewShips.Add(ship);

            ship.SetBoxSelectionPreviewLocal(
                true);
        }
    }

    private bool IsAltPressed()
    {
        return Keyboard.current != null &&
               (Keyboard.current.leftAltKey.isPressed ||
                Keyboard.current.rightAltKey.isPressed);
    }

    private bool IsShipVisibleOnScreen(
    ShipUnit ship)
    {
        if (ship == null ||
            Camera.main == null)
        {
            return false;
        }

        Vector3 screenPosition =
            Camera.main.WorldToScreenPoint(
                ship.transform.position);

        // Za kamer¹.
        if (screenPosition.z <= 0f)
            return false;

        return
            screenPosition.x >= 0f &&
            screenPosition.x <= Screen.width &&
            screenPosition.y >= 0f &&
            screenPosition.y <= Screen.height;
    }

    private void SelectShipsOfSameTypeOnScreen(
    ShipUnit sourceShip,
    bool addToSelection)
    {
        if (sourceShip == null)
            return;

        if (sourceShip.ShipDefinition.shipType == null)
            return;

        var targetType =
            sourceShip.ShipDefinition.shipType;

        // Bez Shift zastêpujemy
        // aktualne zaznaczenie.
        if (!addToSelection)
            ClearSelection();

        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        foreach (ShipUnit ship in allShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsSpawned)
                continue;

            if (ship.isDead.Value)
                continue;

            if (!ship.IsMine())
                continue;

            if (ship.ShipDefinition.shipType == null)
                continue;

            if (ship.ShipDefinition.shipType !=
                targetType)
            {
                continue;
            }

            if (!IsShipVisibleOnScreen(ship))
                continue;

            // Shift = ADD, a nie Toggle.
            if (!selectedShips.Contains(ship))
            {
                selectedShips.Add(ship);

                ship.SetSelectedLocal(true);
            }
        }

        UpdatePrimarySelectedShip();

        selectedBase = null;
        selectedObject = null;

        if (basePanel != null)
            basePanel.SetActive(false);
    }
    private void ShowMoveCommandMarker(
    Vector3 position)
    {
        if (moveCommandMarkerPrefab == null)
            return;

        position.y +=
            moveCommandMarkerHeight;

        Instantiate(
            moveCommandMarkerPrefab,
            position,
            Quaternion.identity);
    }

    private void TryStop()
    {
        if (GameInputManager.Instance == null)
            return;

        if (!GameInputManager.Instance.StopPressed)
            return;

        foreach (ShipUnit ship in selectedShips)
        {
            if (ship == null)
                continue;

            if (!ship.IsMine())
                continue;

            ship.ClearVisualCommands();
            ship.StopServerRpc();
        }
    }

    private void RefreshAttackTargetMarkers()
    {
        ShipUnit[] allShips =
            FindObjectsByType<ShipUnit>(
                FindObjectsSortMode.None);

        // Najpierw wy³¹czamy wszystkie markery.
        foreach (ShipUnit ship in allShips)
        {
            if (ship == null)
                continue;

            ship.SetAttackTargetMarkerLocal(
                false);
        }


        // Nastêpnie szukamy celów Attack
        // zaznaczonych przez nas statków.
        foreach (ShipUnit selectedShip in selectedShips)
        {
            if (selectedShip == null)
                continue;

            foreach (
                ShipUnit.VisualShipCommand command
                in selectedShip.VisualCommands)
            {
                if (command.Type !=
                    ShipUnit.ShipCommandType.Attack)
                {
                    continue;
                }

                ShipUnit targetShip =
                    command.TargetShip;

                if (targetShip == null)
                    continue;

                if (!targetShip.IsSpawned)
                    continue;

                if (targetShip.isDead.Value)
                    continue;

                targetShip.SetAttackTargetMarkerLocal(
                    true);
            }
        }
    }
}
