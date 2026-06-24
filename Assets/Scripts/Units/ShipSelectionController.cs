using UnityEngine;
using UnityEngine.InputSystem;

public class ShipSelectionController : MonoBehaviour
{
    [SerializeField] private float shipFlightHeight = 0.5f;

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

        if (selection == null || movement == null)
        {
            ClearSelection();
            return;
        }

        ClearSelection();

        selectedObject = owner.gameObject;
        selectedSelection = selection;
        selectedMovement = movement;

        selectedSelection.SetSelected(true);
    }

    private void TryMove()
    {
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
    }
}