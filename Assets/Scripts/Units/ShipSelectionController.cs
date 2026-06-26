using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ShipSelectionController : MonoBehaviour
{
    [SerializeField] private float shipFlightHeight = 0.5f;

    [SerializeField] private GameObject basePanel;

    private PlayerBaseUnit selectedBase;

    private GameObject selectedObject;
    private SelectionTarget selectedSelection;
    private UnitMovement selectedMovement;

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
            TrySelect();

        if (Mouse.current.rightButton.wasPressedThisFrame)
            TryMove();
    }

    private void TrySelect()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            ClearSelection();
            return;
        }

        UnitOwner owner = hit.collider.GetComponentInParent<UnitOwner>();

        if (owner == null || !owner.IsMine())
        {
            ClearSelection();
            return;
        }

        SelectionTarget selection = owner.GetComponent<SelectionTarget>();
        UnitMovement movement = owner.GetComponent<UnitMovement>();

        if (selection == null)
        {
            ClearSelection();
            return;
        }

        ClearSelection();

        selectedObject = owner.gameObject;
        selectedSelection = selection;
        selectedMovement = movement;
        selectedSelection.SetSelected(true);

        selectedBase = owner.GetComponent<PlayerBaseUnit>();

        if (basePanel != null)
            basePanel.SetActive(selectedBase != null);
    }

    private void TryMove()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (selectedMovement == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        Vector3 target = hit.point;
        target.y = shipFlightHeight;

        selectedMovement.MoveToServerRpc(target);
    }

    private void ClearSelection()
    {
        if (selectedSelection != null)
            selectedSelection.SetSelected(false);



        selectedObject = null;
        selectedSelection = null;
        selectedMovement = null;
        selectedBase = null;

        if (basePanel != null)
            basePanel.SetActive(false);
    }
}